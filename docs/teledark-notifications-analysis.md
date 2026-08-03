# Как работают уведомления в TeleDark (анализ с нуля)

Источник: `TeleDark [v12.7].ipa` (86 695 323 байт), скачан по ссылке пользователя с
Google Drive. Анализ проведён с нуля: распаковка IPA, разбор `Info.plist`,
`embedded.mobileprovision`, strings/символы Mach-O бинарников и injected dylib.
Ниже только то, что реально подтверждено в файле.

---

## 1. Что за приложение

| Поле | Значение |
|------|----------|
| Тип | Модифицированный клиент Telegram iOS (сайдлоад, не App Store) |
| CFBundleIdentifier | `app.teledark` |
| Версия / Build | 12.7 / 1 |
| CFBundleExecutable | `Telegram` |
| MinimumOSVersion | 10.0 |

Базовый бинарник — форк Telegram iOS 12.7 со штатными фреймворками
(`TelegramCoreFramework`, `TelegramUIFramework`, `PostboxFramework`,
`MtProtoKitFramework`, `SwiftSignalKitFramework`) плюс 15 вшитых dylib.

---

## 2. Архитектура уведомлений (штатная схема Telegram + сайдлоад-фиксы)

Push в Telegram iOS работает не «сам по себе» — входящий APNs payload
приходит зашифрованным, и его расшифровывает **Notification Service Extension**,
читая ключи из общего контейнера **App Group**. Поэтому для рабочих
уведомлений критичны три вещи: (1) entitlement `aps-environment`,
(2) расширения уведомлений, (3) единый App Group между приложением и
расширениями.

### 2.1 Entitlements (из `embedded.mobileprovision`, одинаковы во всех таргетах)

```
aps-environment = production
com.apple.developer.usernotifications.communication  = true
com.apple.developer.usernotifications.time-sensitive = true
com.apple.security.application-groups:
    group.1f56048729375e2d.1 .. .5
com.apple.developer.team-identifier = LKRRVC93D7
```

Без `aps-environment` push на iOS не работает вообще. Бесплатная 7-дневная
подпись (личный Apple ID через AltStore/Sideloadly) обычно НЕ даёт
`aps-environment` — нужен платный Apple Developer с включённой capability
Push Notifications для App ID.

### 2.2 Info.plist главного приложения

```
UIBackgroundModes = [audio, fetch, location, remote-notification, voip, processing]
                                                ^^^^^^^^^^^^^^^^^^^ обязателен для APNs
BGTaskSchedulerPermittedIdentifiers = [app.teledark.refresh, app.teledark.cleanup, app.teledark.upload.*]
```

### 2.3 Расширения (PlugIns/*.appex)

| .appex | Bundle ID | NSExtensionPointIdentifier | Principal class |
|--------|-----------|----------------------------|-----------------|
| NotificationServiceExtensionv1 | `app.teledark.NotificationService` | `com.apple.usernotifications.service` | `NotificationService` |
| NotificationContentExtension | `app.teledark.NotificationContent` | `com.apple.usernotifications.content-extension` | `NotificationViewController` |
| WidgetExtension | `app.teledark.Widget` | `com.apple.widgetkit-extension` | — |
| ShareExtension | `app.teledark.Share` | `com.apple.share-services` | `ShareRootController` |
| IntentsExtension | `app.teledark.SiriIntents` | `com.apple.intents-service` | `IntentHandler` |
| BroadcastUploadExtension | `app.teledark.BroadcastUpload` | `com.apple.broadcast-services-upload` | `BroadcastUploadSampleHandler` |

Критичные для push — первые два. `NotificationService` перехватывает входящий
push до показа и расшифровывает содержимое; `NotificationContent` рисует
кастомный UI (категории reply/mute с медиа).

### 2.4 Общий App Group

И главный бинарник, и `NotificationServiceExtensionv1` ссылаются на один и тот
же App Group `group.1f56048729375e2d.*` (подтверждено strings в обоих Mach-O).
Через него расширение читает ключи расшифровки и пишет обратно данные для
основного приложения. Если App Group не совпадает — уведомления «немые» или
не приходят.

---

## 3. В чём фокус для сайдлоада: fix-dylib

При переподписи модифицированного IPA **своим** сертификатом меняется префикс
App Group (Team ID), и расширения перестают видеть общий контейнер основного
приложения → уведомления ломаются. TeleDark решает это вшитыми dylib.

Порядок загрузки (LC_LOAD_DYLIB, `@executable_path/...`):

```
1 FilePickerFix   2 TelegramChatFixer   3 TelegramNotificationFix   4 sideloadFixerLol
5 libsubstrate    6 liberdade(5.7MB осн.мод)  7 libps  8 flutersult  9 lidersunframe
10 ZiSpop-up      11 iOSGods  12 SatellaJailed  13 putcher_concurent  14 liframesatrate  15 libobjcpatch
```

### 3.1 `TelegramNotificationFix.dylib` (~232 KB, Logos/Substrate)

Подтверждённые хуки (по символам `_logos_method$/_logos_orig$`):

| # | Класс | Метод | Назначение |
|---|-------|-------|------------|
| 1 | `AppDelegate` | `application:didFinishLaunchingWithOptions:` | код при старте (в т.ч. попап автора через FCAlertView) |
| 2 | `NSFileManager` | `containerURLForSecurityApplicationGroupIdentifier:` | **ключевой фикс**: подмена пути к App Group контейнеру, чтобы app и extensions видели общий контейнер после переподписи |
| 3 | `INPreferences` | `_siriAuthorizationStatus` (private) | обход/фикс статуса Siri |
| 4 | `AppDelegate` | `FCAlertView:clickedButtonIndex:buttonTitle:` | обработка кнопок попапа |

Есть неудовлетворённая зависимость `@rpath/nicegram-BlackScreenFIX.dylib`
(файла в бандле нет). Автор строк внутри dylib: `@DzMohaipa`.

### 3.2 `sideloadFixerLol.dylib` (~85 KB)

Дополнительный фикс App Group / CloudKit контейнеров. Использует
`containerURLForSecurityApplicationGroupIdentifier:`, `CKContainer`,
runtime swizzling (`method_getImplementation/method_setImplementation`).
Team ID `LKRRVC93D7` зашит в dylib.

---

## 4. Цепочка push (итоговая)

```
TeleDark(app.teledark) --registerForRemoteNotifications--> Apple APNs (нужен aps-environment)
APNs --device token--> TeleDark --token--> Telegram servers (привязка к bundle id)
Входящий push (зашифрован) --> NotificationServiceExtension расшифровывает,
   читая ключи из App Group group.1f56048729375e2d.* --> показ уведомления
   (+ NotificationContentExtension для кастомного UI)
```

Сайдлоад-фиксы (`TelegramNotificationFix` + `sideloadFixerLol`) обеспечивают,
что после переподписи app и extensions продолжают использовать один общий
App Group контейнер — иначе расшифровка/показ push не работает.

---

## 5. Что нужно, чтобы повторить в своём клиенте

1. Платный Apple Developer App ID с включённой capability **Push Notifications**
   → в профиле появится `aps-environment`.
2. `UIBackgroundModes` c `remote-notification` в Info.plist главного app.
3. **Notification Service Extension** (+ опционально Content Extension) с теми же
   App Groups, что и у основного приложения.
4. Единый **App Group**, прописанный в entitlements app и всех extensions.
5. Для сайдлоада с переподписью — хук
   `NSFileManager containerURLForSecurityApplicationGroupIdentifier:`,
   чтобы app и extensions резолвили один и тот же контейнер, независимо от
   изменившегося Team ID/префикса App Group.
