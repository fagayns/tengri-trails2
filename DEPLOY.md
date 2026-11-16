# Запуск Tengri Trails (демо)

## 1. Telegram-бот
1. В Telegram откройте @BotFather → `/newbot` → получите **токен**.
2. Запустите сайт с токеном (шаг 2 или 3), откройте своего бота и отправьте `/start` — он ответит вашим **chat id**.
3. Задайте chat id (`Telegram__ChatId`) и перезапустите сайт. Теперь:
   - каждая заявка приходит в Telegram сообщением;
   - команда `/bookings` показывает последние 10 заявок.
Токен нельзя хранить в репозитории — только в переменных окружения / user-secrets.

## 2. Локально (нужен .NET 10 SDK)
```bash
dotnet user-secrets set "Telegram:BotToken" "123456:ABC..."
dotnet user-secrets set "Telegram:ChatId" "123456789"
dotnet run
```
Откройте http://localhost:5000 (порт напечатает dotnet). База SQLite `tengri.db` создаётся сама.

## 3. Онлайн (Render + Neon Postgres)
1. Создайте бесплатную базу на neon.tech и скопируйте параметры подключения.
2. Загрузите проект в GitHub (файл `Dockerfile` уже есть).
3. На render.com: New → Web Service → ваш репозиторий → Runtime **Docker**. Health check path: `/healthz`.
4. Environment variables:
   - `Database__Provider` = `Postgres`
   - `ConnectionStrings__Postgres` = `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`
   - `Telegram__BotToken` = токен бота
   - `Telegram__ChatId` = ваш chat id
5. Deploy. Бесплатный Render засыпает после ~15 минут без запросов: откройте сайт перед показом
   (или пингуйте `/healthz` через UptimeRobot). Пока сервис спит, команды бота не отвечают.

## 4. Картинки туров
Положите файлы в `wwwroot/img/tours/` (см. README.txt в этой папке): `slug.jpg` — обложка карточки,
`slug-2.jpg`, `slug-3.jpg` — дополнительные фото галереи. Без файлов остаются встроенные SVG.
