TicketMS/
├── TicketMS.sln
│
├── src/
│   │
│   ├── TicketMS.WebAPI/                    # TIER 1: Presentation
│   │   ├── Controllers/
│   │   │   └── AuthController.cs
│   │   ├── Middleware/
│   │   │   ├── Models/
│   │   │   │   └── ErrorTracker.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   ├── Extensions/
│   │   │   └── ServiceExtensions.cs
│   │   ├── appsettings.json
│   │   ├── Program.cs
│   │   └── TicketMS.WebAPI.csproj
│   │
│   ├── TicketMS.Application/               # TIER 2: Business Logic
│   │   ├── Services/
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── ITokenService.cs
│   │   │   └── TokenService.cs
│   │   ├── DTOs/
│   │   │   ├── RegisterDto.cs
│   │   │   ├── LoginDto.cs
│   │   │   ├── AuthResponseDto.cs
│   │   │   └── UserDto.cs
│   │   ├── Common/
│   │   │   └── ApiResponse.cs
│   │   └── TicketMS.Application.csproj
│   │
│   └── TicketMS.Infrastructure/            # TIER 3: Data Access
│       ├── Data/
│       │   └── ApplicationDbContext.cs
│       ├── Entities/
│       │   ├── ApplicationUser.cs
│       │   ├── ApplicationRole.cs
│       │   └── RefreshToken.cs
│       ├── Migrations/
│       ├── Seed/
│       │   └── DbInitializer.cs
│       └── TicketMS.Infrastructure.csproj
│
└── README.md