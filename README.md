# Schichtpilot

## Links

- [Requirements Document](https://studfhcampuswienac-my.sharepoint.com/:w:/g/personal/emre_rauhofer_stud_hcw_ac_at/ETXIL6jBFTFDqLKIh49uBQ8BKT2v0Ik-Hr-IGqXnaHVf-w?email=thomas.berger1%40edu.hcw.ac.at&e=o7fYHn)
- [Artefakte](https://studfhcampuswienac-my.sharepoint.com/:f:/g/personal/emre_rauhofer_stud_hcw_ac_at/EruIjFSmbaBHtofRKE1OfO4B9s2l31hiNuSx57JHuuh4DQ?email=thomas.berger1%40edu.hcw.ac.at&e=ddHT9j)
- [Trello Board](https://trello.com/b/MMMe7Kjt/schichtpilot-projekt)

## Tools and Technology

.NET 10
EntityFramework with MsSQL server
Identity Server (see link in the next chapter)
Cookie Authentication
Automapper
Docker
Dockerfile
docker-compose

Svelte as the frontend

### Links

- [Web-Api](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Getting started](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli)
- [Identity](https://learn.microsoft.com/de-de/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)
- [Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview)
- [Cookie Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)
- [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)

- [Svelte](https://svelte.dev/docs/svelte/getting-started)

## Communication

- Discord
- Whatsapp

## Coding

- Git
- Github Actions
- Github Projects

### Testing

No TDD required but unit testing should be done.
Atleast 70% code coverage overall.

### Project Issues

All use cases from the requirement document will be broken done into several issues.
These issues all have the same time requirement amount.
For tracking these issues we use a Kanban board in Github Projects.

Code can only be merged after creating a pull request. 
Before commiting anything to the main branch the code needs:

- Unit tests
- No failed tests
- Be reviewed by another person

## Planning

- MsProjects (Timemanagement)
- Trello (Kanban Board)
- Draw.Io (Diagrams)
- mermaid.live (Diagrams)
- Frame0 (Wireframes/Moqups)
- MS Word
- MS Excel

## Storage

- Github (Code)
- OneDrive (Artefacts)
- Discord (Temporary artefacts)
- Whatsapp (Temporary artefacts)

## Docker 

```
docker run -d \
  -p 80:8080 \
  -p 443:8081 \
  -v /home/user/schichtpilot/appsettings.Production.json:/app/appsettings.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  ghcr.io/rauhofere/schichtpilot:latest
```

## .env

This file needs to be in the root of the frontend folder
.env
.env.development
.env.production

```
PUBLIC_BASE_URL='https://localhost:5001'
```

## Appsettings Development

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AuthCookieName": "SchichtPilotAuthCookie",
  "AllowedCors": [
    "http://localhost:4200"
  ],
  "ConnectionStrings": {
    "DefaultConnection": "_dbConnectionString_"
  },
  "AuthenticationSettings": {
    "JwtKey": "_jwtKey_",
    "TokenLifeTimeInMinutes": 720
  },
  "AzureEmail": {
    "ConnectionString": "_connectionstring_",
    "SenderAddress": "_senderAddress_",
    "SendMail": false
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [{
      "Name": "Console"
    },
      {
        "Name": "File",
        "Args": {
          "Path": "/tmp/Schichtpilot.Log/Schichtpilot.Log-.txt",
          "rollingInterval": "Day",
          "Timestapm": "yyyy-mm-dd HH:mm:ss",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.ffff}] [{Level}] {Message}{NewLine:1}{Exception:1}"
        }
      }
    ]
  },
  "Kestrel": {
    "Endpoints": {
      "Https":{
        "Url": "https://0.0.0.0:8081",
        "Certificate": {
          "Path": "_certificate_",
          "Password": "_password_"
        }
      },
      "Http": {
        "Url": "http://0.0.0.0:8080"
      }
    }
  }
}


```