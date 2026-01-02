TicketMS/
├── TicketMS.sln
│
├── src/
│   │
│   ├── TicketMS.WebAPI/                        # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── UsersController.cs
│   │   │   ├── RolesController.cs
│   │   │   ├── ProjectsController.cs
│   │   │   ├── TicketsController.cs
│   │   │   ├── BoardsController.cs
│   │   │   ├── SprintsController.cs
│   │   │   ├── CommentsController.cs
│   │   │   ├── AttachmentsController.cs
│   │   │   ├── WorkflowsController.cs
│   │   │   ├── LabelsController.cs
│   │   │   └── ReportsController.cs
│   │   │
│   │   ├── Middleware/
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   │
│   │   ├── Hubs/
│   │   │   ├── BoardHub.cs
│   │   │   └── NotificationHub.cs
│   │   │
│   │   ├── Extensions/
│   │   │   ├── ServiceExtensions.cs
│   │   │   └── MiddlewareExtensions.cs
│   │   │
│   │   ├── appsettings.json
│   │   ├── Program.cs
│   │   └── TicketMS.WebAPI.csproj
│   │
│   ├── TicketMS.Application/                   # Application Layer
│   │   ├── Services/
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── ITokenService.cs
│   │   │   ├── TokenService.cs
│   │   │   ├── IUserService.cs
│   │   │   ├── UserService.cs
│   │   │   ├── IProjectService.cs
│   │   │   ├── ProjectService.cs
│   │   │   ├── ITicketService.cs
│   │   │   ├── TicketService.cs
│   │   │   ├── IBoardService.cs
│   │   │   ├── BoardService.cs
│   │   │   ├── ISprintService.cs
│   │   │   ├── SprintService.cs
│   │   │   ├── IWorkflowEngine.cs
│   │   │   ├── WorkflowEngine.cs
│   │   │   ├── ICommentService.cs
│   │   │   ├── CommentService.cs
│   │   │   ├── IReportService.cs
│   │   │   └── ReportService.cs
│   │   │
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── LoginDto.cs
│   │   │   │   ├── RegisterDto.cs
│   │   │   │   ├── TokenResponseDto.cs
│   │   │   │   └── ChangePasswordDto.cs
│   │   │   ├── User/
│   │   │   │   ├── UserDto.cs
│   │   │   │   ├── CreateUserDto.cs
│   │   │   │   └── UpdateUserDto.cs
│   │   │   ├── Project/
│   │   │   │   ├── ProjectDto.cs
│   │   │   │   ├── CreateProjectDto.cs
│   │   │   │   └── UpdateProjectDto.cs
│   │   │   ├── Ticket/
│   │   │   │   ├── TicketDto.cs
│   │   │   │   ├── TicketDetailDto.cs
│   │   │   │   ├── CreateTicketDto.cs
│   │   │   │   ├── UpdateTicketDto.cs
│   │   │   │   └── TransitionTicketDto.cs
│   │   │   ├── Board/
│   │   │   │   ├── BoardDto.cs
│   │   │   │   ├── BoardColumnDto.cs
│   │   │   │   └── MoveTicketDto.cs
│   │   │   ├── Sprint/
│   │   │   │   ├── SprintDto.cs
│   │   │   │   ├── CreateSprintDto.cs
│   │   │   │   └── SprintTicketsDto.cs
│   │   │   └── Common/
│   │   │       ├── ApiResponse.cs
│   │   │       └── PagedResult.cs
│   │   │
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs
│   │   │
│   │   ├── Validators/
│   │   │   ├── CreateTicketValidator.cs
│   │   │   └── CreateProjectValidator.cs
│   │   │
│   │   └── TicketMS.Application.csproj
│   │
│   ├── TicketMS.Domain/                        # Domain Layer
│   │   ├── Entities/
│   │   │   ├── ApplicationUser.cs
│   │   │   ├── ApplicationRole.cs
│   │   │   ├── Project.cs
│   │   │   ├── ProjectMember.cs
│   │   │   ├── Ticket.cs
│   │   │   ├── TicketComment.cs
│   │   │   ├── TicketAttachment.cs
│   │   │   ├── TicketLink.cs
│   │   │   ├── TicketHistory.cs
│   │   │   ├── TicketWatcher.cs
│   │   │   ├── Board.cs
│   │   │   ├── BoardColumn.cs
│   │   │   ├── Sprint.cs
│   │   │   ├── Workflow.cs
│   │   │   ├── WorkflowStatus.cs
│   │   │   ├── WorkflowTransition.cs
│   │   │   ├── Label.cs
│   │   │   ├── TimeLog.cs
│   │   │   ├── Notification.cs
│   │   │   └── AuditLog.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── IssueType.cs
│   │   │   ├── Priority.cs
│   │   │   ├── SprintStatus.cs
│   │   │   ├── BoardType.cs
│   │   │   ├── LinkType.cs
│   │   │   └── StatusCategory.cs
│   │   │
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── ICurrentUserService.cs
│   │   │
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   └── AuditableEntity.cs
│   │   │
│   │   └── TicketMS.Domain.csproj
│   │
│   └── TicketMS.Infrastructure/                # Infrastructure Layer
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Configurations/
│       │   │   ├── ApplicationUserConfiguration.cs
│       │   │   ├── ProjectConfiguration.cs
│       │   │   ├── TicketConfiguration.cs
│       │   │   ├── BoardConfiguration.cs
│       │   │   ├── SprintConfiguration.cs
│       │   │   └── WorkflowConfiguration.cs
│       │   └── Repositories/
│       │       ├── Repository.cs
│       │       └── UnitOfWork.cs
│       │
│       ├── Migrations/
│       │   └── (EF Core Migrations)
│       │
│       ├── Identity/
│       │   ├── IdentityService.cs
│       │   └── CurrentUserService.cs
│       │
│       ├── Seed/
│       │   ├── DbInitializer.cs
│       │   ├── RoleSeed.cs
│       │   └── DefaultWorkflowSeed.cs
│       │
│       └── TicketMS.Infrastructure.csproj
│
├── tests/
│   ├── TicketMS.UnitTests/
│   └── TicketMS.IntegrationTests/
│
├── docker-compose.yml
├── Dockerfile
└── README.md