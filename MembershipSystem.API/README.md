# MentorUng Agder – Medlemssystem

Dette er mitt fordypningsprosjekt. Jeg har laget et system for MentorUng Agder som gjør det enklere å administrere medlemmer og ta imot betaling.

---

## Hva gjør systemet?

Når noen fyller ut medlemsskjemaet på Google Forms, blir de automatisk lagt inn i systemet og får tilsendt en betalingslenke på e-post. Etter betaling via Vipps blir medlemskapet aktivert automatisk.

Systemet sender også automatiske påminnelser til medlemmer som snart har utløpende medlemskap.

---

## Teknologier jeg har brukt

- C# og ASP.NET Core 8 for backend
- Entity Framework Core for databasehåndtering
- SQL Server (lokalt med Docker, og Azure SQL i produksjon)
- Hangfire for automatiske jobber
- Vipps for betaling
- JWT for innlogging
- Azure for hosting

---

## Slik kjører du prosjektet lokalt

Du trenger .NET 8 og Docker installert på maskinen.

Start først SQL Server med Docker:
```bash
docker-compose up -d
```

Opprett en fil som heter `appsettings.Development.json` i API-mappen og fyll inn dine egne verdier:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MembershipDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "EN_LANG_HEMMELIG_NOKKEL_HER"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "SenderEmail": "din@epost.no",
    "Password": "ditt-passord",
    "SenderName": "MentorUng Agder"
  },
  "Vipps": {
    "BaseUrl": "https://apitest.vipps.no",
    "ClientId": "DIN_CLIENT_ID",
    "ClientSecret": "DIN_SECRET",
    "SubscriptionKey": "DIN_KEY",
    "MerchantSerialNumber": "DIN_MSN"
  }
}
```

Deretter kjører du prosjektet:
```bash
cd MembershipSystem.API
dotnet run
```

Swagger åpner seg på `http://localhost:5086/swagger` hvor du kan teste alle endepunktene.

---

## Hvordan fungerer betalingen?

Når et nytt medlem registrerer seg, opprettes det automatisk en Vipps-betalingslenke som sendes på e-post. Når betalingen er gjennomført, sender Vipps et callback til API-et og medlemskapet aktiveres med ett års varighet.

---

## Automatiske påminnelser

Jeg har brukt Hangfire til å sende automatiske e-postpåminnelser. Systemet sjekker daglig om noen har medlemskap som snart utløper og sender påminnelse 30 og 7 dager før utløpsdato.

Hangfire-dashboardet kan åpnes på `/hangfire` (krever innlogging).

---

## Produksjon

Systemet kjører på Azure og kan testes her:
- Swagger: https://membership-system-api.azurewebsites.net/swagger

---

## Utvikler

Eren Bostanci – Gokstad Akademiet, 2026