-- =============================================================================
-- Weekly Planning PM - Azure SQL Database
-- Script de creación completo
--
-- Propósito: Soporte a la planificación semanal TENTATIVA del equipo.
--            Este sistema NO reemplaza el sistema de gestión de proyectos.
--            Las asignaciones son tentativas. NO se imputan horas reales.
--            El sistema externo sigue siendo la fuente de verdad.
--
-- Convenciones:
--   - PKs:   UNIQUEIDENTIFIER (GUID), generado por la aplicación.
--   - Dates: DATE para fechas sin hora, DATETIME2 para timestamps UTC.
--   - Soft delete mediante IsActive BIT (no se eliminan registros críticos).
-- =============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- =============================================================================
-- 1. PERSONS
--    Miembros del equipo disponibles para planificación.
--    La capacidad semanal es referencia informativa (no bloquea asignaciones).
-- =============================================================================
CREATE TABLE Persons (
    Id                   UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    Name                 NVARCHAR(200)     NOT NULL,
    Email                NVARCHAR(256)     NOT NULL,
    -- Horas de capacidad semanal declarada (ej: 40). Solo genera advertencias si se supera.
    WeeklyCapacityHours  DECIMAL(5,2)      NOT NULL  DEFAULT 40.00,
    IsActive             BIT               NOT NULL  DEFAULT 1,
    CreatedAt            DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt            DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Persons PRIMARY KEY (Id),
    CONSTRAINT UQ_Persons_Email UNIQUE (Email),
    CONSTRAINT CK_Persons_WeeklyCapacity CHECK (WeeklyCapacityHours > 0 AND WeeklyCapacityHours <= 60)
);
GO

-- =============================================================================
-- 2. EXTERNAL_TASK_SNAPSHOTS
--    Snapshot local de tareas provenientes del sistema externo.
--    Solo se usa para consulta durante la planificación.
--    NUNCA se escribe de vuelta al sistema externo desde esta tabla.
--
--    Campos de horas son INFORMATIVOS (estimaciones del sistema externo).
-- =============================================================================
CREATE TABLE ExternalTaskSnapshots (
    Id                   UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    -- ID de la tarea en el sistema externo. Inmutable una vez creado.
    ExternalTaskId       NVARCHAR(100)     NOT NULL,
    Title                NVARCHAR(500)     NOT NULL,
    Description          NVARCHAR(2000)    NULL,
    ProjectId            NVARCHAR(100)     NULL,
    ProjectName          NVARCHAR(200)     NULL,
    -- Estimación de horas en el sistema externo (fuente de verdad: sistema externo)
    EstimatedHours       DECIMAL(7,2)      NOT NULL  DEFAULT 0.00,
    -- Horas ya imputadas en el sistema externo (solo lectura, referencia para planificación)
    LoggedHours          DECIMAL(7,2)      NOT NULL  DEFAULT 0.00,
    -- Estado de la tarea: 'todo' | 'in_progress' | 'done' | 'blocked'
    Status               NVARCHAR(50)      NOT NULL  DEFAULT 'todo',
    AssigneeExternalId   NVARCHAR(100)     NULL,
    AssigneeName         NVARCHAR(200)     NULL,
    -- Fecha de la última sincronización desde el sistema externo
    LastSyncedAt         DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    -- False si la tarea desapareció del sistema externo en la última sync
    IsActive             BIT               NOT NULL  DEFAULT 1,
    CreatedAt            DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt            DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ExternalTaskSnapshots PRIMARY KEY (Id),
    CONSTRAINT UQ_ExternalTaskSnapshots_ExternalTaskId UNIQUE (ExternalTaskId),
    CONSTRAINT CK_ExternalTaskSnapshots_EstimatedHours CHECK (EstimatedHours >= 0),
    CONSTRAINT CK_ExternalTaskSnapshots_LoggedHours    CHECK (LoggedHours >= 0)
);
GO

CREATE INDEX IX_ExternalTaskSnapshots_IsActive     ON ExternalTaskSnapshots (IsActive);
CREATE INDEX IX_ExternalTaskSnapshots_LastSyncedAt ON ExternalTaskSnapshots (LastSyncedAt DESC);
CREATE INDEX IX_ExternalTaskSnapshots_Status       ON ExternalTaskSnapshots (Status) WHERE IsActive = 1;
GO

-- =============================================================================
-- 3. WEEKLY_PLANS
--    Plan semanal del equipo. Siempre inicia el lunes.
--    Estados: 'Draft' → 'Confirmed' → 'Closed'
--             'Confirmed' puede volver a 'Draft' si se necesitan ajustes.
-- =============================================================================
CREATE TABLE WeeklyPlans (
    Id             UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    -- Debe ser lunes. Restricción reforzada también en la capa de dominio.
    WeekStartDate  DATE              NOT NULL,
    WeekEndDate    DATE              NOT NULL,
    -- 'Draft' | 'Confirmed' | 'Closed'
    Status         NVARCHAR(20)      NOT NULL  DEFAULT 'Draft',
    Notes          NVARCHAR(1000)    NULL,
    CreatedAt      DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt      DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_WeeklyPlans PRIMARY KEY (Id),
    -- Solo puede existir un plan por semana
    CONSTRAINT UQ_WeeklyPlans_WeekStartDate UNIQUE (WeekStartDate),
    CONSTRAINT CK_WeeklyPlans_Status CHECK (Status IN ('Draft', 'Confirmed', 'Closed')),
    CONSTRAINT CK_WeeklyPlans_Dates  CHECK (WeekEndDate > WeekStartDate)
);
GO

-- =============================================================================
-- 4. TASK_ASSIGNMENTS
--    Asignación TENTATIVA de una tarea (del sistema externo) a una persona,
--    dentro de un plan semanal.
--
--    Reglas clave:
--    - PlannedHours son TENTATIVAS, no implican imputación real.
--    - Una misma tarea externa no puede asignarse dos veces a la misma persona
--      en el mismo plan (unicidad por plan + persona + tarea externa).
--    - No se puede asignar en un plan con Status = 'Closed'.
--      (Reforzado en capa de dominio/aplicación.)
-- =============================================================================
CREATE TABLE TaskAssignments (
    Id               UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    WeeklyPlanId     UNIQUEIDENTIFIER  NOT NULL,
    PersonId         UNIQUEIDENTIFIER  NOT NULL,
    -- Referencia de solo lectura a la tarea en el sistema externo
    ExternalTaskId   NVARCHAR(100)     NOT NULL,
    -- Título cacheado localmente para visualización (no es fuente de verdad)
    TaskTitle        NVARCHAR(300)     NOT NULL,
    -- Horas planificadas tentativas para esta semana. NO son horas imputadas.
    PlannedHours     DECIMAL(5,2)      NOT NULL,
    Notes            NVARCHAR(500)     NULL,
    CreatedAt        DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_TaskAssignments PRIMARY KEY (Id),
    -- Evita duplicar la misma tarea para la misma persona en el mismo plan
    CONSTRAINT UQ_TaskAssignments_Plan_Person_Task
        UNIQUE (WeeklyPlanId, PersonId, ExternalTaskId),
    CONSTRAINT CK_TaskAssignments_PlannedHours
        CHECK (PlannedHours > 0 AND PlannedHours <= 40),

    CONSTRAINT FK_TaskAssignments_WeeklyPlan
        FOREIGN KEY (WeeklyPlanId) REFERENCES WeeklyPlans (Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_TaskAssignments_Person
        FOREIGN KEY (PersonId) REFERENCES Persons (Id)
        ON DELETE NO ACTION
);
GO

CREATE INDEX IX_TaskAssignments_PersonId     ON TaskAssignments (PersonId);
CREATE INDEX IX_TaskAssignments_WeeklyPlanId ON TaskAssignments (WeeklyPlanId);
GO

-- =============================================================================
-- 5. PERSONAL_WEEKLY_PLANS
--    Plan semanal personal del PM.
--    Independiente del plan del equipo. Permite al PM organizar su propia semana:
--    reuniones, revisiones, tareas propias, etc.
--
--    Las entradas son libres (no necesariamente tareas del sistema externo).
--    Puede opcionalmente referenciar una tarea externa (ExternalTaskId nullable).
-- =============================================================================
CREATE TABLE PersonalWeeklyPlans (
    Id             UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    -- PM dueño del plan personal
    OwnerId        UNIQUEIDENTIFIER  NOT NULL,
    -- Semana a la que corresponde (debe ser lunes)
    WeekStartDate  DATE              NOT NULL,
    WeekEndDate    DATE              NOT NULL,
    Notes          NVARCHAR(1000)    NULL,
    -- 'Draft' | 'Confirmed'
    Status         NVARCHAR(20)      NOT NULL  DEFAULT 'Draft',
    CreatedAt      DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt      DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PersonalWeeklyPlans PRIMARY KEY (Id),
    -- Un PM solo tiene un plan personal por semana
    CONSTRAINT UQ_PersonalWeeklyPlans_Owner_Week UNIQUE (OwnerId, WeekStartDate),
    CONSTRAINT CK_PersonalWeeklyPlans_Status CHECK (Status IN ('Draft', 'Confirmed')),
    CONSTRAINT CK_PersonalWeeklyPlans_Dates  CHECK (WeekEndDate > WeekStartDate),

    CONSTRAINT FK_PersonalWeeklyPlans_Owner
        FOREIGN KEY (OwnerId) REFERENCES Persons (Id)
        ON DELETE NO ACTION
);
GO

CREATE INDEX IX_PersonalWeeklyPlans_OwnerId ON PersonalWeeklyPlans (OwnerId);
GO

-- =============================================================================
-- 6. PERSONAL_PLAN_ITEMS
--    Ítems individuales dentro del plan personal del PM.
--    Categorías: 'Meeting' | 'Review' | 'Task' | 'Admin' | 'Other'
--
--    Si el ítem corresponde a una tarea del sistema externo, se guarda
--    ExternalTaskId como referencia informativa (sin FK para no acoplar).
-- =============================================================================
CREATE TABLE PersonalPlanItems (
    Id                    UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWID(),
    PersonalWeeklyPlanId  UNIQUEIDENTIFIER  NOT NULL,
    Title                 NVARCHAR(300)     NOT NULL,
    Description           NVARCHAR(1000)    NULL,
    -- 'Meeting' | 'Review' | 'Task' | 'Admin' | 'Other'
    Category              NVARCHAR(50)      NOT NULL  DEFAULT 'Task',
    -- Horas estimadas para este ítem (tentativas, no imputadas)
    EstimatedHours        DECIMAL(5,2)      NULL,
    -- Día de la semana planeado: 1=Lunes ... 5=Viernes (NULL = sin día asignado)
    PlannedDayOfWeek      TINYINT           NULL,
    -- Referencia opcional a tarea del sistema externo (sin FK, referencia informativa)
    ExternalTaskId        NVARCHAR(100)     NULL,
    SortOrder             INT               NOT NULL  DEFAULT 0,
    IsDone                BIT               NOT NULL  DEFAULT 0,
    CreatedAt             DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
    UpdatedAt             DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PersonalPlanItems PRIMARY KEY (Id),
    CONSTRAINT CK_PersonalPlanItems_Category
        CHECK (Category IN ('Meeting', 'Review', 'Task', 'Admin', 'Other')),
    CONSTRAINT CK_PersonalPlanItems_EstimatedHours
        CHECK (EstimatedHours IS NULL OR (EstimatedHours > 0 AND EstimatedHours <= 40)),
    CONSTRAINT CK_PersonalPlanItems_DayOfWeek
        CHECK (PlannedDayOfWeek IS NULL OR PlannedDayOfWeek BETWEEN 1 AND 5),

    CONSTRAINT FK_PersonalPlanItems_PersonalWeeklyPlan
        FOREIGN KEY (PersonalWeeklyPlanId) REFERENCES PersonalWeeklyPlans (Id)
        ON DELETE CASCADE
);
GO

CREATE INDEX IX_PersonalPlanItems_PlanId ON PersonalPlanItems (PersonalWeeklyPlanId);
GO

-- =============================================================================
-- RESUMEN DE RELACIONES
-- =============================================================================
--
--  Persons ──────────────────────────────────────────────────┐
--    │                                                        │
--    │ 1:N (via PersonId)                                     │ 1:N (via OwnerId)
--    ▼                                                        ▼
--  TaskAssignments ◄──── WeeklyPlans          PersonalWeeklyPlans
--    │                                           │
--    │  ExternalTaskId (referencia libre,        │ 1:N
--    │  sin FK, no acoplada al snapshot)         ▼
--    │                                        PersonalPlanItems
--    ▼                                           │
--  ExternalTaskSnapshots ◄─────────────────────── ExternalTaskId (opcional, sin FK)
--    (sincronizado por Azure Function,
--     solo lectura del sistema externo)
--
-- =============================================================================
