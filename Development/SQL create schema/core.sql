/* ============================================================================
   SCHEMA: dùng dbo mặc định. Đổi tên schema nếu bạn muốn.
============================================================================ */

-- BUILDING (global)
CREATE TABLE dbo.building (
                              id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_building PRIMARY KEY DEFAULT NEWID(),
                              code            NVARCHAR(30)     NOT NULL,
                              schema_name     NVARCHAR(128)    NOT NULL,
                              building_name   NVARCHAR(150)    NOT NULL,
                              status          TINYINT          NOT NULL DEFAULT 1,      -- 1: active, 0: inactive (tuỳ bạn định nghĩa)
                              create_at       DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                              update_at       DATETIME2(3)     NULL,
                              CONSTRAINT UQ_building_code       UNIQUE (code),
                              CONSTRAINT UQ_building_schema     UNIQUE (schema_name)
);
GO

-- USER REGISTRY (global)
CREATE TABLE dbo.user_registry (
                                   id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_user_registry PRIMARY KEY DEFAULT NEWID(),
                                   keycloak_user_id UNIQUEIDENTIFIER NOT NULL,
                                   username         NVARCHAR(50)     NOT NULL,
                                   email            NVARCHAR(100)    NOT NULL,
                                   status           TINYINT          NOT NULL DEFAULT 1,
                                   create_at        DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                   update_at        DATETIME2(3)     NULL,
                                   CONSTRAINT UQ_user_registry_keycloak  UNIQUE (keycloak_user_id),
                                   CONSTRAINT UQ_user_registry_username  UNIQUE (username),
                                   CONSTRAINT UQ_user_registry_email     UNIQUE (email)
);
GO

-- USER ↔ BUILDING (gán user vào toà nhà)
CREATE TABLE dbo.user_building (
                                   id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_user_building PRIMARY KEY DEFAULT NEWID(),
                                   keycloak_user_id UNIQUEIDENTIFIER NOT NULL,
                                   building_id      UNIQUEIDENTIFIER NOT NULL,
                                   status           TINYINT          NOT NULL DEFAULT 1,
                                   create_at        DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                   update_at        DATETIME2(3)     NULL,
    -- tránh trùng gán một user vào cùng 1 building nhiều lần
                                   CONSTRAINT UQ_user_building_user_building UNIQUE (keycloak_user_id, building_id),
                                   CONSTRAINT FK_user_building_user_registry  FOREIGN KEY (keycloak_user_id)
                                       REFERENCES dbo.user_registry (keycloak_user_id) ON DELETE CASCADE,
                                   CONSTRAINT FK_user_building_building       FOREIGN KEY (building_id)
                                       REFERENCES dbo.building (id)
);
GO

-- ANNOUNCEMENT (global)
CREATE TABLE dbo.announcement_global (
                                         id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_announcement_global PRIMARY KEY DEFAULT NEWID(),
                                         title          NVARCHAR(200)    NOT NULL,
                                         content        NVARCHAR(MAX)    NOT NULL,
                                         targets        NVARCHAR(500)    NULL,         -- có thể lưu JSON list role/building,… tuỳ thiết kế
                                         schedule_start DATETIME2(3)     NULL,
                                         schedule_end   DATETIME2(3)     NULL,
                                         status         TINYINT          NOT NULL DEFAULT 0,  -- 0: draft, 1: scheduled, 2: sent, …
                                         created_by     UNIQUEIDENTIFIER NULL,         -- keycloak_user_id của người tạo
                                         created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                         updated_at     DATETIME2(3)     NULL,
                                         CONSTRAINT FK_announcement_created_by FOREIGN KEY (created_by)
                                             REFERENCES dbo.user_registry (keycloak_user_id)
);
GO

-- AUDIT LOG (global)
CREATE TABLE dbo.audit_log_global (
                                      id               BIGINT          NOT NULL IDENTITY(1,1) CONSTRAINT PK_audit_log_global PRIMARY KEY,
                                      actor_keycloak_id UNIQUEIDENTIFIER NULL,    -- ai thực hiện
                                      action           NVARCHAR(100)   NOT NULL,  -- CRUD / LOGIN / APPROVE / …
                                      target_type      NVARCHAR(50)    NULL,      -- bảng/đối tượng bị tác động
                                      building_id      UNIQUEIDENTIFIER NULL,     -- liên quan toà nhà nào (nếu có)
                                      schedule_end     DATETIME2(3)    NULL,      -- theo ERD (nếu dùng cho job/schedule)
                                      payload          NVARCHAR(MAX)   NULL,      -- dữ liệu trước/sau (JSON)
                                      ip               VARCHAR(45)     NULL,      -- IPv4/IPv6
                                      ua               NVARCHAR(255)   NULL,      -- user-agent
                                      created_at       DATETIME2(3)    NOT NULL DEFAULT SYSDATETIME(),
                                      CONSTRAINT FK_audit_actor_user    FOREIGN KEY (actor_keycloak_id)
                                          REFERENCES dbo.user_registry (keycloak_user_id),
                                      CONSTRAINT FK_audit_building      FOREIGN KEY (building_id)
                                          REFERENCES dbo.building (id)
);
GO

/* ============================================================================
   INDEXES gợi ý để tối ưu truy vấn
============================================================================ */
CREATE INDEX IX_user_building_building  ON dbo.user_building (building_id);
CREATE INDEX IX_user_building_user      ON dbo.user_building (keycloak_user_id);

CREATE INDEX IX_announcement_status_time ON dbo.announcement_global (status, schedule_start, schedule_end);

CREATE INDEX IX_audit_created_at        ON dbo.audit_log_global (created_at);
CREATE INDEX IX_audit_actor             ON dbo.audit_log_global (actor_keycloak_id);
CREATE INDEX IX_audit_building          ON dbo.audit_log_global (building_id);
GO
