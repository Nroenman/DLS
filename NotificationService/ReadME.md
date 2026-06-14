# Notification Service

En lille .NET-service, der lytter på en RabbitMQ-kø og sender e-mails via Brevo SMTP.

## Hvad gør servicen?

- Lytter på RabbitMQ-køen `Notification`
- Modtager beskeder i JSON-format
- Sender e-mail til den angivne modtager
- Kvitterer beskeden, når e-mailen er sendt
- Afviser beskeden ved fejl

## Beskedformat

```json
{
  "fromName": "System",
  "toEmail": "bruger@firma.dk",
  "subject": "Velkommen",
  "body": "<p>Hej!</p>"
}
