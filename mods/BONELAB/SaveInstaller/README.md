# SaveInstaller (BONELAB MelonLoader mod)

Скачивает файл сохранения по ссылке из `url.txt` и ставит его как `slot_0.save.json`,
удалив остальные `slot_*.save.json`. Работает один раз при старте игры (`OnInitializeMelon`),
пока не изменишь `url.txt`.

Собрано и проверено компиляцией против настоящей `MelonLoader.dll` (net8, v0.6.5)
из `third_party/MelonLoader-bin/net8/` в этом репозитории. Не тестировалось на
реальном устройстве/игре — только компиляция подтверждена.

## Установка (через VR CONTROLER, без ADB — папка вне Android/data)

1. Положи `SaveInstaller.dll` → `MelonLoader/Mods/`
2. Создай `MelonLoader/Mods/SaveInstaller/url.txt` с прямой ссылкой на файл сохранения
   (zip или голый `.json`)
3. Запусти BONELAB — при первом старте мод скачает и установит сохранение
4. Лог результата — в консоли MelonLoader (`MelonLoader/Latest.log`)

## Пересборка

```bash
cd mods/BONELAB/SaveInstaller
dotnet build -c Release SaveInstaller.csproj
```

Нужны референсы `MelonLoader.dll` и `0Harmony.dll` рядом (см. `SaveInstaller.csproj`).
