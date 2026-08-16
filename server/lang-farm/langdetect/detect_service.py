#!/usr/bin/env python3
"""Aggressive language-ID HTTP service for lang-farm bots.

POST /detect  multipart file=*.wav  →  {language, confidence, text, ok, ...}
GET  /health                        →  gates + readiness
"""
from __future__ import annotations

import argparse
import io
import os
import tempfile
import threading
import time
from typing import Any

import numpy as np
from flask import Flask, jsonify, request

app = Flask(__name__)

# --- aggressive defaults (override via env) ---
MIN_CONFIDENCE = float(os.environ.get("MIN_CONFIDENCE", "0.35"))
MIN_SPEECH_SEC = float(os.environ.get("MIN_SPEECH_SEC", "0.8"))
MIN_SPEECH_SEC_NON_EN = float(os.environ.get("MIN_SPEECH_SEC_NON_EN", "1.2"))
ECAPA_MARGIN = float(os.environ.get("ECAPA_MARGIN", "0.08"))
MMS_MARGIN = float(os.environ.get("MMS_MARGIN", "0.05"))
WHISPER_MODEL = os.environ.get("WHISPER_MODEL", "base")
WHISPER_DEVICE = os.environ.get("WHISPER_DEVICE", "cpu")
WHISPER_COMPUTE = os.environ.get("WHISPER_COMPUTE_TYPE", "int8")

_lock = threading.Lock()
_whisper = None
_ecapa = None
_mms = None
_ready = False
_load_error: str | None = None


def _env_gates() -> dict[str, float]:
    return {
        "min_speech_sec": MIN_SPEECH_SEC,
        "min_speech_sec_non_en": MIN_SPEECH_SEC_NON_EN,
        "ecapa_margin": ECAPA_MARGIN,
        "mms_margin": MMS_MARGIN,
    }


def load_models() -> None:
    global _whisper, _ecapa, _mms, _ready, _load_error
    try:
        from faster_whisper import WhisperModel

        print(f"[detect] loading whisper model={WHISPER_MODEL} device={WHISPER_DEVICE}...", flush=True)
        _whisper = WhisperModel(
            WHISPER_MODEL,
            device=WHISPER_DEVICE,
            compute_type=WHISPER_COMPUTE,
        )
        print("[detect] whisper ready", flush=True)

        # Optional LID helpers — best-effort; whisper alone is enough.
        try:
            import torch
            from speechbrain.inference.classifiers import EncoderClassifier

            print("[detect] loading ECAPA lang-id...", flush=True)
            _ecapa = EncoderClassifier.from_hparams(
                source="speechbrain/lang-id-voxlingua107-ecapa",
                savedir=os.path.expanduser("~/.cache/langdetect/ecapa"),
                run_opts={"device": "cpu"},
            )
            print("[detect] ECAPA ready", flush=True)
        except Exception as e:
            print(f"[detect] ECAPA skip: {type(e).__name__}: {e}", flush=True)
            _ecapa = None

        try:
            from transformers import pipeline

            print("[detect] loading MMS LID...", flush=True)
            _mms = pipeline(
                "audio-classification",
                model="facebook/mms-lid-126",
                device=-1,
            )
            print("[detect] MMS ready", flush=True)
        except Exception as e:
            print(f"[detect] MMS skip: {type(e).__name__}: {e}", flush=True)
            _mms = None

        _ready = True
        _load_error = None
        print("[detect] READY (aggressive gates)", flush=True)
    except Exception as e:
        _ready = False
        _load_error = f"{type(e).__name__}: {e}"
        print(f"[detect] LOAD FAIL: {_load_error}", flush=True)
        raise


def _read_wav_mono16k(data: bytes, fallback_sr: int = 48000) -> tuple[np.ndarray, int]:
    import soundfile as sf

    audio, sr = sf.read(io.BytesIO(data), always_2d=False)
    if getattr(audio, "ndim", 1) > 1:
        audio = np.mean(audio, axis=1)
    audio = np.asarray(audio, dtype=np.float32)
    if sr != 16000:
        # lightweight resample
        import librosa

        audio = librosa.resample(audio, orig_sr=sr, target_sr=16000)
        sr = 16000
    # peak normalize (aggressive — keep quiet speech)
    peak = float(np.max(np.abs(audio)) or 1.0)
    if peak > 0:
        audio = audio / peak * 0.95
    return audio, sr


def _speech_seconds(audio: np.ndarray, sr: int) -> float:
    # energy VAD — aggressive (low threshold)
    frame = max(1, int(0.02 * sr))
    if len(audio) < frame:
        return 0.0
    n = len(audio) // frame
    frames = audio[: n * frame].reshape(n, frame)
    rms = np.sqrt(np.mean(frames * frames, axis=1) + 1e-12)
    thr = max(0.008, float(np.percentile(rms, 35)) * 0.55)
    return float(np.sum(rms > thr) * frame / sr)


def _whisper_detect(audio: np.ndarray, sr: int) -> dict[str, Any]:
    assert _whisper is not None
    segments, info = _whisper.transcribe(
        audio,
        language=None,
        task="transcribe",
        vad_filter=True,
        vad_parameters=dict(min_silence_duration_ms=250, speech_pad_ms=120),
        beam_size=1,
        best_of=1,
        temperature=0.0,
        condition_on_previous_text=False,
    )
    texts = []
    for seg in segments:
        t = (seg.text or "").strip()
        if t:
            texts.append(t)
    text = " ".join(texts).strip()
    lang = (getattr(info, "language", None) or "unknown") or "unknown"
    conf = float(getattr(info, "language_probability", 0.0) or 0.0)
    return {"language": lang, "confidence": conf, "text": text}


def _ecapa_detect(audio: np.ndarray, sr: int) -> tuple[str | None, float]:
    if _ecapa is None:
        return None, 0.0
    import torch

    wav = torch.from_numpy(audio).float().unsqueeze(0)
    try:
        out = _ecapa.classify_batch(wav)
        # speechbrain returns (out_prob, score, index, text_lab) depending on version
        if isinstance(out, (list, tuple)) and len(out) >= 4:
            text_lab = out[3]
            score = out[1]
            if hasattr(text_lab, "__getitem__"):
                lab = text_lab[0] if len(text_lab) else None
            else:
                lab = str(text_lab)
            if hasattr(score, "__getitem__"):
                sc = float(score[0].item() if hasattr(score[0], "item") else score[0])
            else:
                sc = float(score)
            # labels like 'en: English' or 'en'
            if lab is None:
                return None, 0.0
            lab = str(lab)
            code = lab.split(":")[0].split(" ")[0].strip().lower()
            return code or None, sc
    except Exception as e:
        print(f"[detect] ecapa err: {e}", flush=True)
    return None, 0.0


def _mms_detect(audio: np.ndarray, sr: int) -> tuple[str | None, float]:
    if _mms is None:
        return None, 0.0
    try:
        preds = _mms({"array": audio, "sampling_rate": sr}, top_k=3)
        if not preds:
            return None, 0.0
        top = preds[0]
        lab = str(top.get("label", "")).lower()
        score = float(top.get("score", 0.0))
        # normalize eng_Latn → en etc.
        code = lab.split("_")[0][:3]
        mapping = {
            "eng": "en",
            "rus": "ru",
            "spa": "es",
            "fra": "fr",
            "deu": "de",
            "por": "pt",
            "pol": "pl",
            "ukr": "uk",
            "ita": "it",
            "tur": "tr",
            "jpn": "ja",
            "cmn": "zh",
            "kor": "ko",
            "ara": "ar",
            "nld": "nl",
        }
        code = mapping.get(code, code[:2] if len(code) >= 2 else code)
        return code or None, score
    except Exception as e:
        print(f"[detect] mms err: {e}", flush=True)
    return None, 0.0


def _agree(a: str | None, b: str | None) -> bool:
    if not a or not b:
        return False
    return a.lower()[:2] == b.lower()[:2]


def detect_audio(data: bytes, min_confidence: float, sr_hint: int) -> dict[str, Any]:
    audio, sr = _read_wav_mono16k(data, fallback_sr=sr_hint or 48000)
    speech = _speech_seconds(audio, sr)
    total = len(audio) / float(sr)

    with _lock:
        w = _whisper_detect(audio, sr)
        e_lang, e_score = _ecapa_detect(audio, sr)
        m_lang, m_score = _mms_detect(audio, sr)

    lang = w["language"]
    conf = float(w["confidence"])
    text = w["text"]

    # Boost when secondary LID agrees (aggressive — small margins).
    votes = [lang]
    if e_lang:
        votes.append(e_lang)
        if _agree(lang, e_lang):
            conf = min(0.99, conf + max(0.0, e_score) * 0.15 + ECAPA_MARGIN)
        elif e_score > conf + ECAPA_MARGIN and e_score > 0.55:
            lang, conf = e_lang, e_score
    if m_lang:
        votes.append(m_lang)
        if _agree(lang, m_lang):
            conf = min(0.99, conf + max(0.0, m_score) * 0.12 + MMS_MARGIN)
        elif m_score > conf + MMS_MARGIN and m_score > 0.55:
            lang, conf = m_lang, m_score

    need = MIN_SPEECH_SEC_NON_EN if (lang or "").lower()[:2] not in ("en",) else MIN_SPEECH_SEC
    ok = bool(
        lang
        and lang.lower() not in ("unknown", "und", "")
        and conf >= min_confidence
        and speech >= need
    )

    return {
        "ok": ok,
        "language": lang or "unknown",
        "confidence": round(conf, 4),
        "text": text,
        "speech_sec": round(speech, 3),
        "audio_sec": round(total, 3),
        "votes": votes,
        "ecapa": {"language": e_lang, "score": round(e_score, 4)} if e_lang else None,
        "mms": {"language": m_lang, "score": round(m_score, 4)} if m_lang else None,
        "min_confidence": min_confidence,
        "gates": _env_gates(),
    }


@app.get("/health")
def health():
    return jsonify(
        {
            "ok": _ready,
            "ready": _ready,
            "error": _load_error,
            "min_confidence": MIN_CONFIDENCE,
            "gates": _env_gates(),
            "whisper_model": WHISPER_MODEL,
            "ecapa": _ecapa is not None,
            "mms": _mms is not None,
            "aggressive": True,
        }
    )


@app.post("/detect")
def detect():
    if not _ready:
        return jsonify({"ok": False, "language": "unknown", "confidence": 0, "text": "", "error": "not_ready"}), 503
    min_conf = float(request.args.get("min_confidence", MIN_CONFIDENCE))
    sr_hint = int(float(request.args.get("sr", "48000")))
    f = request.files.get("file") or request.files.get("audio")
    if f is None:
        return jsonify({"ok": False, "error": "missing file"}), 400
    data = f.read()
    if not data:
        return jsonify({"ok": False, "error": "empty"}), 400
    try:
        result = detect_audio(data, min_conf, sr_hint)
        return jsonify(result)
    except Exception as e:
        return jsonify({"ok": False, "language": "unknown", "confidence": 0, "text": "", "error": str(e)}), 500


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=8091)
    ap.add_argument("--preload", action="store_true")
    args = ap.parse_args()
    if args.preload:
        load_models()
    else:
        threading.Thread(target=load_models, daemon=True).start()
    app.run(host=args.host, port=args.port, threaded=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
