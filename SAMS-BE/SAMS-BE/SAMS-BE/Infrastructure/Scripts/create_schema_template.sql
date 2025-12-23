
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{{SCHEMA}}')
BEGIN
    EXEC('CREATE SCHEMA [{{SCHEMA}}]');
END
GO

/* ============================================================================

   1) LOOKUP / DICTIONARIES
============================================================================ */

CREATE TABLE [{{SCHEMA}}].[users](
    user_id UNIQUEIDENTIFIER NOT NULL,
    username NVARCHAR(50) NOT NULL,
    email NVARCHAR(50) NOT NULL,
    phone NVARCHAR(20) NOT NULL,
    first_name NVARCHAR(100) NOT NULL,
    last_name NVARCHAR(100) NOT NULL,
    dob DATE,
    address NVARCHAR(150),
    avatar_url NVARCHAR(300) NULL,
    checkin_photo_url NVARCHAR(300) NULL,
    face_embedding VARBINARY(MAX) NULL,
    created_at DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2(3) NULL,
    CONSTRAINT PK_users PRIMARY KEY (user_id),
    CONSTRAINT UQ_users_username UNIQUE (username),
    CONSTRAINT UQ_users_email UNIQUE (email)
)

CREATE TABLE [{{SCHEMA}}].[staff_profiles] (
                                         staff_code       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                         user_id          UNIQUEIDENTIFIER NULL,
                                         hire_date        DATE             NULL,
                                         termination_date DATE             NULL,
                                         notes            NVARCHAR(1000)   NULL,
                                         CONSTRAINT PK_staff_profiles PRIMARY KEY (staff_code),
                                         CONSTRAINT FK_staff_profiles_user
                                             FOREIGN KEY (user_id) REFERENCES [{{SCHEMA}}].users(user_id)
);

CREATE TABLE [{{SCHEMA}}].[asset_categories] (
                                           category_id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                           code               NVARCHAR(64)     NOT NULL,
                                           name               NVARCHAR(255)    NOT NULL,
                                           description        NVARCHAR(1000)   NULL,
                                           maintenance_frequency INT            NULL,
                                           default_reminder_days INT NULL DEFAULT 3,
                                           CONSTRAINT PK_asset_categories PRIMARY KEY (category_id),
                                           CONSTRAINT UQ_asset_categories_code UNIQUE (code)
);
GO

CREATE TABLE [{{SCHEMA}}].[vehicle_types] (
                                        vehicle_type_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                        code            NVARCHAR(64)     NOT NULL,
                                        name            NVARCHAR(255)    NOT NULL,
                                        CONSTRAINT PK_vehicle_types PRIMARY KEY (vehicle_type_id),
                                        CONSTRAINT UQ_vehicle_types_code UNIQUE (code)
);
GO

CREATE TABLE [{{SCHEMA}}].[payment_methods] (
                                          payment_method_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                          code              NVARCHAR(64)     NOT NULL,
                                          name              NVARCHAR(255)    NOT NULL,
                                          active            BIT              NOT NULL DEFAULT (1),
                                          CONSTRAINT PK_payment_methods PRIMARY KEY (payment_method_id),
                                          CONSTRAINT UQ_payment_methods_code UNIQUE (code)
);
GO

CREATE TABLE [{{SCHEMA}}].[service_types] (
                                        service_type_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                        code            NVARCHAR(64)     NOT NULL,
                                        name            NVARCHAR(255)    NOT NULL,
                                        category        NVARCHAR(64)     NULL,    -- ví dụ: UTILITY / MGMT / PARKING ...
                                        unit            NVARCHAR(64)     NULL,    -- m3, kWh, tháng, lần, ...
                                        is_mandatory    BIT              NOT NULL DEFAULT (0),
                                        is_recurring    BIT              NOT NULL DEFAULT (1),
                                        is_active       BIT              NOT NULL DEFAULT (1),
                                        created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                        updated_at      DATETIME2(3)     NULL,
                                        CONSTRAINT PK_service_types PRIMARY KEY (service_type_id),
                                        CONSTRAINT UQ_service_types_code UNIQUE (code)
);
GO


CREATE TABLE [{{SCHEMA}}].[floors] (
                                 floor_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                 floor_number INT              NOT NULL,
                                 name         NVARCHAR(255)    NULL,
                                 CONSTRAINT PK_floors PRIMARY KEY (floor_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[apartments] (
                                     apartment_id    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                     floor_id        UNIQUEIDENTIFIER NOT NULL,
                                     number          NVARCHAR(64)     NOT NULL,
                                     area_m2         DECIMAL(10,2)    NULL,
                                     bedrooms        INT              NULL,
                                     status          NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                     image NVARCHAR(250),
                                     type NVARCHAR(100),
                                     created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                     created_by      NVARCHAR(190)    NULL,
                                     updated_at      DATETIME2(3)     NULL,
                                     updated_by      NVARCHAR(190)    NULL,
                                     CONSTRAINT PK_apartments PRIMARY KEY (apartment_id),
                                     CONSTRAINT FK_apartments_floor FOREIGN KEY (floor_id)
                                         REFERENCES [{{SCHEMA}}].floors(floor_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[resident_profiles] (
                                            resident_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                            user_id         UNIQUEIDENTIFIER NULL,
                                            full_name       NVARCHAR(255)    NOT NULL,
                                            phone           NVARCHAR(50)     NULL,
                                            email           NVARCHAR(190)    NULL,
                                            id_number       NVARCHAR(64)     NULL,
                                            dob             DATE             NULL,
                                            gender          NVARCHAR(16)     NULL,
                                            address         NVARCHAR(500)    NULL,
                                            status          NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                            meta            NVARCHAR(MAX)    NULL,
                                            created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                            updated_at      DATETIME2(3)     NULL,
                                            CONSTRAINT PK_resident_profiles PRIMARY KEY (resident_id),
                                            CONSTRAINT UQ_resident_profiles_user UNIQUE (user_id),
                                            CONSTRAINT FK_resident_profiles_user FOREIGN KEY (user_id)
                                                REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[resident_apartments] (
                                              resident_apartment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                              resident_id           UNIQUEIDENTIFIER NOT NULL,
                                              apartment_id          UNIQUEIDENTIFIER NOT NULL,
                                              relation_type         NVARCHAR(32)     NOT NULL,
                                              start_date            DATE             NOT NULL,
                                              end_date              DATE             NULL,
                                              is_primary            BIT              NOT NULL DEFAULT (0),
                                              CONSTRAINT PK_resident_apartments PRIMARY KEY (resident_apartment_id),
                                              CONSTRAINT FK_ra_resident FOREIGN KEY (resident_id)
                                                  REFERENCES [{{SCHEMA}}].resident_profiles(resident_id) ON DELETE CASCADE,
                                              CONSTRAINT FK_ra_apartment FOREIGN KEY (apartment_id)
                                                  REFERENCES [{{SCHEMA}}].apartments(apartment_id) ON DELETE CASCADE,
                                              CONSTRAINT UQ_ra_unique UNIQUE (resident_id, apartment_id, relation_type, start_date)
);
GO

CREATE TABLE [{{SCHEMA}}].tickets (
                                  ticket_id      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                  created_by_user_id UNIQUEIDENTIFIER NULL,
                                  category       NVARCHAR(64)     NOT NULL,
                                  priority       NVARCHAR(32)     NOT NULL DEFAULT N'NORMAL',
                                  subject        NVARCHAR(255)    NOT NULL,
                                  description    NVARCHAR(MAX)    NULL,
                                  status         NVARCHAR(32)     NOT NULL DEFAULT N'OPEN',
                                  expected_completion_at DATETIME2(3) NULL,
                                  created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                  updated_at     DATETIME2(3)     NULL,
                                  closed_at      DATETIME2(3)     NULL,
                                  scope          NVARCHAR(64)     NULL,
                                  apartment_id   UNIQUEIDENTIFIER NULL,
                                  has_invoice    BIT NOT NULL DEFAULT 0,
                                  CONSTRAINT PK_tickets PRIMARY KEY (ticket_id),
                                  CONSTRAINT FK_tickets_users FOREIGN KEY (created_by_user_id)
                                    REFERENCES [{{SCHEMA}}].users(user_id),
                                  CONSTRAINT FK_tickets_apartment FOREIGN KEY (apartment_id)
                                    REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

CREATE TABLE [{{SCHEMA}}].maintenance_apartment_history (
                                                        id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                                        apartment_id     UNIQUEIDENTIFIER NOT NULL,
                                                        creator_user_id  UNIQUEIDENTIFIER NOT NULL,
                                                        handler_user_id  UNIQUEIDENTIFIER NULL,
                                                        request_id       UNIQUEIDENTIFIER NULL,
                                                        request_type     NVARCHAR(64)     NOT NULL,
                                                        target_department NVARCHAR(128)   NULL,
                                                        description      NVARCHAR(MAX)    NULL,
                                                        creation_time    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                                        priority         NVARCHAR(32)     NOT NULL DEFAULT N'NORMAL',
                                                        status           NVARCHAR(32)     NOT NULL DEFAULT N'OPEN',
                                                        attachment       NVARCHAR(1000)   NULL,
                                                        sla_due_time     DATETIME2(3)     NULL,

                                                        CONSTRAINT PK_mah PRIMARY KEY (id),
                                                        CONSTRAINT FK_mah_apartment FOREIGN KEY (apartment_id)
                                                            REFERENCES [{{SCHEMA}}].apartments(apartment_id),
                                                        CONSTRAINT FK_mah_creator FOREIGN KEY (creator_user_id)
                                                            REFERENCES [{{SCHEMA}}].users(user_id),
                                                        CONSTRAINT FK_mah_handler FOREIGN KEY (handler_user_id)
                                                            REFERENCES [{{SCHEMA}}].users(user_id),
                                                        CONSTRAINT FK_mah_request FOREIGN KEY (request_id)
                                                            REFERENCES [{{SCHEMA}}].tickets(ticket_id),
                                                        CONSTRAINT CK_mah_priority CHECK (priority IN (N'LOW',N'NORMAL',N'HIGH',N'URGENT')),
                                                        CONSTRAINT CK_mah_status   CHECK (status IN (N'OPEN',N'IN_PROGRESS',N'RESOLVED',N'CLOSED',N'CANCELLED'))
);
GO

/* ============================================================================
   3) CARDS / VEHICLES / PARKING / ACCESS
============================================================================ */
CREATE TABLE [{{SCHEMA}}].access_cards (
                                       card_id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                       card_number           NVARCHAR(128)    NOT NULL,       -- Mã số thẻ vật lý
                                       status                NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE', -- ACTIVE, INACTIVE, EXPIRED, LOST...
                                       issued_to_user_id     UNIQUEIDENTIFIER NULL,            -- FK -> [{{SCHEMA}}].users (nếu gắn user)
                                       issued_to_apartment_id UNIQUEIDENTIFIER NULL,           -- FK -> [{{SCHEMA}}].apartments (nếu gắn cư dân)
                                       issued_date           DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                       expired_date          DATETIME2(3)     NULL,
                                       created_at            DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                       updated_at            DATETIME2(3)     NULL,
                                       created_by            NVARCHAR(190)    NULL,
                                       updated_by            NVARCHAR(190)    NULL,
                                       is_delete             BIT              NOT NULL DEFAULT 0,

                                       CONSTRAINT PK_access_cards PRIMARY KEY (card_id),
                                       CONSTRAINT UQ_access_cards_number UNIQUE (card_number),

                                       CONSTRAINT FK_access_cards_user FOREIGN KEY (issued_to_user_id)
                                           REFERENCES [{{SCHEMA}}].users(user_id),

                                       CONSTRAINT FK_access_cards_apartment FOREIGN KEY (issued_to_apartment_id)
                                           REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

--Card
CREATE TABLE [{{SCHEMA}}].access_card_types (
                                            card_type_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                            code           NVARCHAR(50)     NOT NULL,
                                            name           NVARCHAR(100)    NOT NULL,
                                            description    NVARCHAR(255)    NULL,
                                            is_active      BIT              NOT NULL DEFAULT 1,   -- Có cho phép sử dụng quyền này hay không
                                            is_delete      BIT              NOT NULL DEFAULT 0,   -- Xóa mềm
                                            created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                            updated_at     DATETIME2(3)     NULL,
                                            created_by     NVARCHAR(190)    NULL,
                                            updated_by     NVARCHAR(190)    NULL,
                                            CONSTRAINT PK_access_card_types PRIMARY KEY (card_type_id),
                                            CONSTRAINT UQ_access_card_types_code UNIQUE (code)
);
GO

CREATE TABLE [{{SCHEMA}}].access_card_capabilities (
                                                   card_capability_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                                   card_id            UNIQUEIDENTIFIER NOT NULL,       -- FK -> access_cards
                                                   card_type_id       UNIQUEIDENTIFIER NOT NULL,       -- FK -> access_card_types
                                                   is_enabled         BIT              NOT NULL DEFAULT 1,  -- Có thể khóa từng chức năng riêng
                                                   valid_from         DATETIME2(3)     NULL,            -- Ngày bắt đầu hiệu lực (optional)
                                                   valid_to           DATETIME2(3)     NULL,            -- Ngày hết hạn riêng (optional)
                                                   created_at         DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                                   updated_at         DATETIME2(3)     NULL,
                                                   created_by         NVARCHAR(190)    NULL,
                                                   updated_by         NVARCHAR(190)    NULL,

                                                   CONSTRAINT PK_access_card_capabilities PRIMARY KEY (card_capability_id),
                                                   CONSTRAINT UQ_access_card_capabilities UNIQUE (card_id, card_type_id), -- 1 thẻ chỉ có 1 dòng/loại
                                                   CONSTRAINT FK_acc_card FOREIGN KEY (card_id)
                                                       REFERENCES [{{SCHEMA}}].access_cards(card_id) ON DELETE CASCADE,
                                                   CONSTRAINT FK_acc_type FOREIGN KEY (card_type_id)
                                                       REFERENCES [{{SCHEMA}}].access_card_types(card_type_id) ON DELETE CASCADE
);
GO

CREATE TABLE [{{SCHEMA}}].[card_history] (
                                           [card_history_id] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
                                           [card_id] UNIQUEIDENTIFIER NOT NULL,
                                           [card_type_id] UNIQUEIDENTIFIER NULL,
                                           [event_code] NVARCHAR(64) NOT NULL,
                                           [event_time_utc] DATETIME2(3) NOT NULL DEFAULT (DATEADD(HOUR, 7, SYSUTCDATETIME())),
                                           [field_name] NVARCHAR(128) NULL,
                                           [old_value] NVARCHAR(500) NULL,
                                           [new_value] NVARCHAR(500) NULL,
                                           [description] NVARCHAR(255) NULL,
                                           [valid_from] DATETIME2(3) NULL,
                                           [valid_to] DATETIME2(3) NULL,
                                           [created_by] NVARCHAR(190) NULL,
                                           [created_at] DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
                                           [is_delete] BIT NOT NULL DEFAULT 0,

                                           CONSTRAINT [PK_card_history] PRIMARY KEY ([card_history_id]),

                                           CONSTRAINT [FK_card_history_card] FOREIGN KEY ([card_id])
                                               REFERENCES [{{SCHEMA}}].[access_cards] ([card_id]) ON DELETE CASCADE,

                                           CONSTRAINT [FK_card_history_card_type] FOREIGN KEY ([card_type_id])
                                               REFERENCES [{{SCHEMA}}].[access_card_types] ([card_type_id]) ON DELETE SET NULL
);
GO

CREATE TABLE [{{SCHEMA}}].[vehicles] (
                                   vehicle_id       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                   resident_id      UNIQUEIDENTIFIER NULL,
                                   apartment_id     UNIQUEIDENTIFIER NULL,
                                   vehicle_type_id  UNIQUEIDENTIFIER NOT NULL,
                                   license_plate    NVARCHAR(64)     NOT NULL,
                                   color            NVARCHAR(64)     NULL,
                                   brand_model      NVARCHAR(128)    NULL,
                                   parking_card_id  UNIQUEIDENTIFIER NULL,
                                   registered_at    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                   status           NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                   meta             NVARCHAR(MAX)    NULL,
                                   CONSTRAINT PK_vehicles PRIMARY KEY (vehicle_id),
                                   CONSTRAINT UQ_vehicles_plate UNIQUE (license_plate),
                                   CONSTRAINT FK_vehicles_resident FOREIGN KEY (resident_id)
                                       REFERENCES [{{SCHEMA}}].resident_profiles(resident_id),
                                   CONSTRAINT FK_vehicles_apartment FOREIGN KEY (apartment_id)
                                       REFERENCES [{{SCHEMA}}].apartments(apartment_id),
                                   CONSTRAINT FK_vehicles_type FOREIGN KEY (vehicle_type_id)
                                       REFERENCES [{{SCHEMA}}].vehicle_types(vehicle_type_id),
                                   CONSTRAINT FK_vehicles_card FOREIGN KEY (parking_card_id)
                                       REFERENCES [{{SCHEMA}}].access_cards(card_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[parking_entries] (
                                          parking_entry_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                          entry_time       DATETIME2(3)     NOT NULL,
                                          exit_time        DATETIME2(3)     NULL,
                                          card_id          UNIQUEIDENTIFIER NULL,
                                          vehicle_id       UNIQUEIDENTIFIER NULL,
                                          plate_snapshot   NVARCHAR(255)    NULL,
                                          entry_gate       NVARCHAR(64)     NULL,
                                          exit_gate        NVARCHAR(64)     NULL,
                                          fee_amount       DECIMAL(18,2)    NULL,
                                          fee_currency     CHAR(3)          NULL DEFAULT 'VND',
                                          created_at       DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                          CONSTRAINT PK_parking_entries PRIMARY KEY (parking_entry_id),
                                          CONSTRAINT FK_parking_entries_card FOREIGN KEY (card_id)
                                              REFERENCES [{{SCHEMA}}].access_cards(card_id),
                                          CONSTRAINT FK_parking_entries_vehicle FOREIGN KEY (vehicle_id)
                                              REFERENCES [{{SCHEMA}}].vehicles(vehicle_id)
);
GO


--Asset
CREATE TABLE [{{SCHEMA}}].assets (
                                asset_id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                category_id     UNIQUEIDENTIFIER NOT NULL,
                                code            NVARCHAR(64)     NOT NULL,
                                name            NVARCHAR(255)    NOT NULL,
                                apartment_id    UNIQUEIDENTIFIER NULL,
                                block_id        UNIQUEIDENTIFIER NULL,
                                location        NVARCHAR(255)    NULL,
                                purchase_date   DATE             NULL,
                                warranty_expire DATE             NULL,
                                maintenance_frequency INT NULL,
                                status          NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                is_delete       BIT              NOT NULL DEFAULT 0,
                                CONSTRAINT PK_assets PRIMARY KEY (asset_id),
                                CONSTRAINT UQ_assets_code UNIQUE (code),
                                CONSTRAINT FK_assets_category FOREIGN KEY (category_id)
                                    REFERENCES [{{SCHEMA}}].asset_categories(category_id),
                                CONSTRAINT FK_assets_apartment FOREIGN KEY (apartment_id)
                                    REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

/* ============================================================================
   4) AMENITIES & BOOKINGS
============================================================================ */
CREATE TABLE [{{SCHEMA}}].[amenities] (
                                    amenity_id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                    asset_id            UNIQUEIDENTIFIER NULL,
                                    code                NVARCHAR(64)     NOT NULL,
                                    name                NVARCHAR(255)    NOT NULL,
                                    category_name       NVARCHAR(100)    NULL,
                                    location            NVARCHAR(255)    NULL,
                                    has_monthly_package BIT NOT NULL DEFAULT 1,
                                    fee_type            NVARCHAR(20) NOT NULL DEFAULT N'Paid',
                                    status              NVARCHAR(32) NOT NULL DEFAULT N'ACTIVE',
                                    is_delete           BIT NOT NULL DEFAULT 0,
                                    requires_face_verification BIT NOT NULL DEFAULT 0,
                                    CONSTRAINT PK_amenities PRIMARY KEY (amenity_id),
                                     CONSTRAINT UQ_amenities_code UNIQUE (code),
                                    CONSTRAINT FK_amenities_asset FOREIGN KEY (asset_id) REFERENCES [{{SCHEMA}}].assets(asset_id)
);
GO

CREATE TABLE [{{SCHEMA}}].amenity_packages (
                                    package_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                    amenity_id   UNIQUEIDENTIFIER NOT NULL,
                                    name         NVARCHAR(100)    NOT NULL,
                                    month_count  INT              NOT NULL,
                                    duration_days INT NULL,
                                    period_unit  NVARCHAR(10)     NULL,
                                    price        INT              NOT NULL,
                                    description  NVARCHAR(500)    NULL,
                                    status       NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                    CONSTRAINT PK_amenity_packages PRIMARY KEY(package_id),
                                    CONSTRAINT FK_ap_amenity FOREIGN KEY(amenity_id) REFERENCES [{{SCHEMA}}].amenities(amenity_id) ON DELETE CASCADE
);
GO

CREATE TABLE [{{SCHEMA}}].[amenity_bookings] (
                                           booking_id      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                           amenity_id      UNIQUEIDENTIFIER NOT NULL,
                                            package_id      UNIQUEIDENTIFIER NOT NULL,
                                            apartment_id    UNIQUEIDENTIFIER NOT NULL,
                                            user_id         UNIQUEIDENTIFIER NULL,
                                            start_date      DATE NOT NULL,
                                            end_date        DATE NOT NULL,
                                            price           INT  NOT NULL DEFAULT 0,
                                            total_price     INT  NOT NULL DEFAULT 0,
                                            status          NVARCHAR(32) NOT NULL DEFAULT N'Pending',
                                            payment_status  NVARCHAR(32) NOT NULL DEFAULT N'Unpaid',
                                            notes           NVARCHAR(1000) NULL,
                                            created_at      DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                            created_by      NVARCHAR(190) NULL,
                                            updated_at      DATETIME2(3) NULL,
                                            updated_by      NVARCHAR(190) NULL,
                                            is_delete       BIT NOT NULL DEFAULT 0,
                                            CONSTRAINT PK_amenity_bookings PRIMARY KEY (booking_id),
                                            CONSTRAINT FK_ab_amenity FOREIGN KEY (amenity_id)
                                                REFERENCES [{{SCHEMA}}].amenities(amenity_id),
                                            CONSTRAINT FK_ab_package FOREIGN KEY (package_id)
                                                REFERENCES [{{SCHEMA}}].amenity_packages(package_id),
                                            CONSTRAINT FK_ab_apartment FOREIGN KEY (apartment_id)
                                                REFERENCES [{{SCHEMA}}].apartments(apartment_id),
                                            CONSTRAINT FK_ab_user FOREIGN KEY (user_id)
                                                REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

CREATE TABLE [{{SCHEMA}}].amenity_check_ins
(
                                            check_in_id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                            booking_id               UNIQUEIDENTIFIER NOT NULL,
                                            checked_in_for_user_id   UNIQUEIDENTIFIER NOT NULL,
                                            checked_in_by_user_id    UNIQUEIDENTIFIER NULL,
                                            similarity               FLOAT            NULL,
                                            is_success               BIT              NOT NULL DEFAULT (1),
                                            result_status            NVARCHAR(32)     NOT NULL DEFAULT N'Success',
                                            message                  NVARCHAR(500)    NULL,
                                            captured_image_url       NVARCHAR(500)    NULL,
                                            is_manual_override       BIT              NOT NULL DEFAULT (0),
                                            checked_in_at            DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                            created_at               DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                            created_by               NVARCHAR(190)    NULL,
                                            CONSTRAINT PK_amenity_check_ins PRIMARY KEY (check_in_id),
                                            CONSTRAINT FK_amenity_check_ins_booking
                                                FOREIGN KEY (booking_id) REFERENCES [{{SCHEMA}}].amenity_bookings(booking_id) ON DELETE CASCADE,
                                            CONSTRAINT FK_amenity_check_ins_checked_for
                                                FOREIGN KEY (checked_in_for_user_id) REFERENCES [{{SCHEMA}}].users(user_id) ON DELETE NO ACTION,
                                            CONSTRAINT FK_amenity_check_ins_checked_by
                                                FOREIGN KEY (checked_in_by_user_id) REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

/* ============================================================================
   5) SERVICES, PRICES, SUBSCRIPTIONS, METERS & READINGS
============================================================================ */
CREATE TABLE [{{SCHEMA}}].[service_prices] (
                                         service_prices          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                         service_type_id UNIQUEIDENTIFIER NOT NULL,
                                         unit_price      DECIMAL(18,6)    NOT NULL,
                                         effective_date  DATE             NOT NULL,
                                         end_date        DATE             NULL,
                                         status          NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                         created_by      UNIQUEIDENTIFIER NULL,
                                         approved_by     UNIQUEIDENTIFIER NULL,
                                         approved_date   DATETIME2(3)     NULL,
                                         notes           NVARCHAR(1000)   NULL,
                                         created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                         updated_at      DATETIME2(3)     NULL,
                                         CONSTRAINT PK_service_prices PRIMARY KEY (service_prices),
                                         CONSTRAINT FK_fee_service_type FOREIGN KEY (service_type_id)
                                             REFERENCES [{{SCHEMA}}].service_types(service_type_id),
                                         CONSTRAINT CK_fee_dates CHECK (end_date IS NULL OR end_date > effective_date),
                                         CONSTRAINT FK_fee_created_by  FOREIGN KEY (created_by)  REFERENCES [{{SCHEMA}}].staff_profiles(staff_code),
                                         CONSTRAINT FK_fee_approved_by FOREIGN KEY (approved_by) REFERENCES [{{SCHEMA}}].staff_profiles(staff_code)
);
GO

CREATE TABLE [{{SCHEMA}}].[apartment_services] (
                                             apartment_service_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                             apartment_id         UNIQUEIDENTIFIER NOT NULL,
                                             service_id           UNIQUEIDENTIFIER NOT NULL,
                                             start_date           DATE             NOT NULL,
                                             end_date             DATE             NULL,
                                             billing_cycle        NVARCHAR(16)     NOT NULL DEFAULT N'MONTHLY',
                                             quantity             DECIMAL(18,4)    NULL,
                                             status               NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                             meta                 NVARCHAR(MAX)    NULL,
                                             CONSTRAINT PK_apartment_services PRIMARY KEY (apartment_service_id),
                                             CONSTRAINT UQ_apartment_services UNIQUE (apartment_id, service_id, start_date),
                                             CONSTRAINT FK_as_apartment FOREIGN KEY (apartment_id)
                                                 REFERENCES [{{SCHEMA}}].apartments(apartment_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_as_service FOREIGN KEY (service_id)
                                                 REFERENCES [{{SCHEMA}}].service_types(service_type_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[meters] (
                                 meter_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                 apartment_id UNIQUEIDENTIFIER NOT NULL,
                                 service_id   UNIQUEIDENTIFIER NOT NULL,
                                 serial_no    NVARCHAR(128)    NOT NULL,
                                 installed_at DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                 status       NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                 meta         NVARCHAR(MAX)    NULL,
                                 CONSTRAINT PK_meters PRIMARY KEY (meter_id),
                                 CONSTRAINT UQ_meters UNIQUE (apartment_id, service_id, serial_no),
                                 CONSTRAINT FK_meters_apartment FOREIGN KEY (apartment_id)
                                     REFERENCES [{{SCHEMA}}].apartments(apartment_id) ON DELETE CASCADE,
                                 CONSTRAINT FK_meters_service FOREIGN KEY (service_id)
                                     REFERENCES [{{SCHEMA}}].service_types(service_type_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[meter_readings] (
                                         reading_id    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                         meter_id      UNIQUEIDENTIFIER NOT NULL,
                                         reading_time  DATETIME2(3)     NOT NULL,
                                         index_value   DECIMAL(18,6)    NOT NULL,
                                         captured_by   NVARCHAR(190)    NULL,
                                         note          NVARCHAR(500)    NULL,
                                         created_at    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                         CONSTRAINT PK_meter_readings PRIMARY KEY (reading_id),
                                         CONSTRAINT UQ_meter_readings UNIQUE (meter_id, reading_time),
                                         CONSTRAINT FK_meter_readings_meter FOREIGN KEY (meter_id)
                                             REFERENCES [{{SCHEMA}}].meters(meter_id) ON DELETE CASCADE
);
GO


/* ============================================================================
   7) TICKETS / MAINTENANCE / APPOINTMENTS / ACTION LOG
============================================================================ */

CREATE TABLE [{{SCHEMA}}].ticket_comments (
                                          comment_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                          ticket_id    UNIQUEIDENTIFIER NOT NULL,
                                          commented_by UNIQUEIDENTIFIER NULL,
                                          comment_time DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                          content      NVARCHAR(MAX)    NOT NULL,
                                          CONSTRAINT PK_ticket_comments PRIMARY KEY (comment_id),
                                          CONSTRAINT FK_ticket_comments_ticket FOREIGN KEY (ticket_id)
                                              REFERENCES [{{SCHEMA}}].tickets(ticket_id) ON DELETE CASCADE
);
GO

CREATE TABLE [{{SCHEMA}}].appointments (
                                       appointment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                       ticket_id      UNIQUEIDENTIFIER NULL,
                                       apartment_id   UNIQUEIDENTIFIER NOT NULL,
                                       start_at       DATETIME2(3)     NOT NULL,
                                       end_at         DATETIME2(3)     NOT NULL,
                                       location       NVARCHAR(255)    NULL,
                                       created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                       CONSTRAINT PK_appointments PRIMARY KEY (appointment_id),
                                       CONSTRAINT FK_appointments_ticket FOREIGN KEY (ticket_id)
                                           REFERENCES [{{SCHEMA}}].tickets(ticket_id),
                                       CONSTRAINT FK_appointments_apartment FOREIGN KEY (apartment_id)
                                           REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

CREATE TABLE [{{SCHEMA}}].asset_maintenance_schedule (
                                schedule_id    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                asset_id       UNIQUEIDENTIFIER NOT NULL,
                                start_date     DATE NOT NULL,
                                end_date       DATE NOT NULL,
                                start_time     TIME NULL,
                                end_time       TIME NULL,
                                reminder_days  INT NOT NULL,
                                description    NVARCHAR(500) NULL,
                                created_by     UNIQUEIDENTIFIER NULL,
                                created_at     DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                status         NVARCHAR(32) NOT NULL,
                                recurrence_type NVARCHAR(32) NULL,
                                recurrence_interval INT NULL,
                                scheduled_start_date DATE NULL,
                                scheduled_end_date   DATE NULL,
                                actual_start_date    DATETIME2(3) NULL,
                                actual_end_date      DATETIME2(3) NULL,
                                completion_notes     NVARCHAR(1000) NULL,
                                completed_by         UNIQUEIDENTIFIER NULL,
                                completed_at         DATETIME2(3) NULL,
                                CONSTRAINT PK_asset_maintenance_schedule PRIMARY KEY (schedule_id),
                                CONSTRAINT FK_ams_asset FOREIGN KEY (asset_id) REFERENCES [{{SCHEMA}}].assets(asset_id) ON DELETE CASCADE,
                                CONSTRAINT FK_ams_created_by FOREIGN KEY (created_by) REFERENCES [{{SCHEMA}}].users(user_id),
                                CONSTRAINT FK_ams_completed_by FOREIGN KEY (completed_by) REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

/* ============================================================================
   6) INVOICING & PAYMENTS
============================================================================ */

CREATE TABLE [{{SCHEMA}}].asset_maintenance_history (
                                                    history_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                                    asset_id       UNIQUEIDENTIFIER NOT NULL,
                                                    schedule_id    UNIQUEIDENTIFIER NULL,
                                                    action_date    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                                    action         NVARCHAR(255)    NOT NULL,
                                                    cost_amount    DECIMAL(18,2)    NULL,
                                                    notes          NVARCHAR(1000)   NULL,
                                                    next_due_date  DATE             NULL,
                                                    actual_start_date DATETIME2(3) NULL,
                                                    actual_end_date   DATETIME2(3) NULL,
                                                    scheduled_start_date DATE NULL,
                                                    scheduled_end_date   DATE NULL,
                                                    completion_status NVARCHAR(32) NULL,
                                                    days_difference INT NULL,
                                                    performed_by   UNIQUEIDENTIFIER NULL,
                                                    CONSTRAINT PK_asset_maintenance_history PRIMARY KEY (history_id),
                                                    CONSTRAINT FK_amh_asset FOREIGN KEY (asset_id)
                                                        REFERENCES [{{SCHEMA}}].assets(asset_id) ON DELETE CASCADE,
                                                    CONSTRAINT FK_amh_schedule FOREIGN KEY (schedule_id)
                                                        REFERENCES [{{SCHEMA}}].asset_maintenance_schedule(schedule_id),
                                                    CONSTRAINT FK_amh_performed_by FOREIGN KEY (performed_by)
                                                        REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

CREATE TABLE [{{SCHEMA}}].[vouchers] (
                                    voucher_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                    voucher_number NVARCHAR(64)     NOT NULL,
                                    company_info   NVARCHAR(MAX)    NULL,
                                    type           NVARCHAR(32)     NOT NULL,
                                    date           DATE             NOT NULL,
                                    total_amount   DECIMAL(18,2)    NOT NULL,
                                    description    NVARCHAR(1000)   NULL,
                                    status         NVARCHAR(32)     NOT NULL DEFAULT N'DRAFT',
                                    created_by     UNIQUEIDENTIFIER NULL,
                                    approved_by    UNIQUEIDENTIFIER NULL,
                                    approved_date  DATETIME2(3)     NULL,
                                    created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                    ticket_id      UNIQUEIDENTIFIER NULL,
                                    history_id     UNIQUEIDENTIFIER NULL,
                                    CONSTRAINT PK_vouchers PRIMARY KEY (voucher_id),
                                    CONSTRAINT UQ_vouchers_number UNIQUE (voucher_number),
                                    CONSTRAINT CK_vouchers_company_json CHECK (company_info IS NULL OR ISJSON(company_info)=1),
                                    CONSTRAINT FK_vouchers_created_by FOREIGN KEY (created_by)  REFERENCES [{{SCHEMA}}].staff_profiles(staff_code),
                                    CONSTRAINT FK_vouchers_approved_by FOREIGN KEY (approved_by) REFERENCES [{{SCHEMA}}].staff_profiles(staff_code),
                                    CONSTRAINT FK_vouchers_ticket FOREIGN KEY (ticket_id) REFERENCES [{{SCHEMA}}].tickets(ticket_id),
                                    CONSTRAINT FK_vouchers_history FOREIGN KEY (history_id) REFERENCES [{{SCHEMA}}].asset_maintenance_history(history_id)
);
GO

CREATE TABLE [{{SCHEMA}}].voucher_items (
                                        voucher_items_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                        voucher_id       UNIQUEIDENTIFIER NOT NULL,
                                        description      NVARCHAR(500)   NULL,
                                        quantity         DECIMAL(18,2)   NULL,
                                        unit_price       DECIMAL(18,2)   NULL,
                                        amount           DECIMAL(18,2)   NULL,
                                        service_type_id  UNIQUEIDENTIFIER NULL,
                                        apartment_id     UNIQUEIDENTIFIER NULL,
                                        created_at       DATETIME2(3)    NOT NULL DEFAULT SYSDATETIME(),
                                        CONSTRAINT PK_voucher_items PRIMARY KEY (voucher_items_id),
                                        CONSTRAINT FK_vi_voucher FOREIGN KEY (voucher_id)
                                            REFERENCES [{{SCHEMA}}].vouchers(voucher_id) ON DELETE CASCADE,
                                        CONSTRAINT FK_vi_service_type FOREIGN KEY (service_type_id)
                                            REFERENCES [{{SCHEMA}}].service_types(service_type_id),
                                        CONSTRAINT FK_vi_apartment FOREIGN KEY (apartment_id)
                                            REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

CREATE TABLE [{{SCHEMA}}].journal_entries (
                                          entry_id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                          entry_number      NVARCHAR(64)     NOT NULL,
                                          entry_date        DATE             NOT NULL,
                                          reference_type    NVARCHAR(32)     NULL,
                                          reference_id      UNIQUEIDENTIFIER NULL,
                                          description       NVARCHAR(1000)   NULL,
                                          total_debit       DECIMAL(18,2)    NOT NULL DEFAULT (0),
                                          total_credit      DECIMAL(18,2)    NOT NULL DEFAULT (0),
                                          status            NVARCHAR(16)     NOT NULL DEFAULT N'DRAFT',
                                          posted_by         UNIQUEIDENTIFIER NULL,
                                          posted_date       DATETIME2(3)     NULL,
                                          reversed_by       UNIQUEIDENTIFIER NULL,
                                          reversed_date     DATETIME2(3)     NULL,
                                          reversal_entry_id UNIQUEIDENTIFIER NULL,
                                          created_by        UNIQUEIDENTIFIER NULL,
                                          created_at        DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),

                                          CONSTRAINT PK_journal_entries PRIMARY KEY (entry_id),
                                          CONSTRAINT UQ_journal_entries_number UNIQUE (entry_number),
                                          CONSTRAINT CK_je_totals_nonneg CHECK (total_debit >= 0 AND total_credit >= 0),
                                          CONSTRAINT FK_je_created_by  FOREIGN KEY (created_by)  REFERENCES [{{SCHEMA}}].staff_profiles(staff_code),
                                          CONSTRAINT FK_je_posted_by   FOREIGN KEY (posted_by)   REFERENCES [{{SCHEMA}}].staff_profiles(staff_code),
                                          CONSTRAINT FK_je_reversed_by FOREIGN KEY (reversed_by) REFERENCES [{{SCHEMA}}].staff_profiles(staff_code)
);
GO

CREATE TABLE [{{SCHEMA}}].journal_entry_lines (
                                              line_id       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                              entry_id      UNIQUEIDENTIFIER NOT NULL,
                                              line_number   INT              NOT NULL,
                                              account_code  NVARCHAR(64)     NOT NULL,
                                              description   NVARCHAR(500)    NULL,
                                              debit_amount  DECIMAL(18,2)    NULL,
                                              credit_amount DECIMAL(18,2)    NULL,
                                              apartment_id  UNIQUEIDENTIFIER NULL,
                                              department    NVARCHAR(128)    NULL,
                                              cost_center   NVARCHAR(128)    NULL,
                                              created_at    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                              CONSTRAINT PK_journal_entry_lines PRIMARY KEY (line_id),
                                              CONSTRAINT FK_jel_entry FOREIGN KEY (entry_id)
                                                  REFERENCES [{{SCHEMA}}].journal_entries(entry_id) ON DELETE CASCADE,
                                              CONSTRAINT UQ_jel_entry_line UNIQUE (entry_id, line_number),
                                              CONSTRAINT CK_jel_amounts CHECK (
                                                  (debit_amount  IS NULL OR debit_amount  >= 0) AND
                                                  (credit_amount IS NULL OR credit_amount >= 0) AND
                                                  NOT (ISNULL(debit_amount,0)=0 AND ISNULL(credit_amount,0)=0)
                                                  ),
                                              CONSTRAINT FK_jel_apartment FOREIGN KEY (apartment_id)
                                                  REFERENCES [{{SCHEMA}}].apartments(apartment_id)
);
GO

CREATE TABLE [{{SCHEMA}}].invoices (
                                    invoice_id       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                    invoice_no       NVARCHAR(64)     NOT NULL,
                                    apartment_id     UNIQUEIDENTIFIER NOT NULL,
                                    issue_date       DATE             NOT NULL,
                                    due_date         DATE             NOT NULL,
                                    status           NVARCHAR(32)     NOT NULL DEFAULT N'PENDING',
                                    subtotal_amount  DECIMAL(18,2)    NOT NULL DEFAULT (0),
                                    tax_amount       DECIMAL(18,2)    NOT NULL DEFAULT (0),
                                    total_amount     DECIMAL(18,2)    NOT NULL DEFAULT (0),
                                    note             NVARCHAR(1000)   NULL,
                                    created_at       DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                    created_by       NVARCHAR(190)    NULL,
                                    updated_at       DATETIME2(3)     NULL,
                                    updated_by       NVARCHAR(190)    NULL,
                                    ticket_id        UNIQUEIDENTIFIER NULL,
                                    CONSTRAINT PK_invoices PRIMARY KEY (invoice_id),
                                    CONSTRAINT UQ_invoices_no UNIQUE (invoice_no),
                                    CONSTRAINT FK_invoices_apartment FOREIGN KEY (apartment_id)
                                        REFERENCES [{{SCHEMA}}].apartments(apartment_id),
                                    CONSTRAINT FK_invoices_ticket FOREIGN KEY (ticket_id)
                                        REFERENCES [{{SCHEMA}}].tickets(ticket_id)
);
GO

CREATE TABLE [{{SCHEMA}}].invoice_details (
                                        invoice_detail_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                        invoice_id        UNIQUEIDENTIFIER NOT NULL,
                                        service_id        UNIQUEIDENTIFIER NOT NULL,
                                        description       NVARCHAR(255)    NULL,
                                        quantity          DECIMAL(18,6)    NOT NULL DEFAULT (1),
                                        unit_price        DECIMAL(18,6)    NOT NULL DEFAULT (0),
                                        amount            DECIMAL(37,12)   NULL,
                                        vat_rate          DECIMAL(5,2)     NULL,
                                        vat_amount        DECIMAL(18,2)    NULL,
                                        CONSTRAINT PK_invoice_details PRIMARY KEY (invoice_detail_id),
                                        CONSTRAINT FK_invoice_details_invoice FOREIGN KEY (invoice_id)
                                            REFERENCES [{{SCHEMA}}].invoices(invoice_id) ON DELETE CASCADE,
                                        CONSTRAINT FK_invoice_details_service FOREIGN KEY (service_id)
                                            REFERENCES [{{SCHEMA}}].service_types(service_type_id)
);
GO

CREATE TABLE [{{SCHEMA}}].receipts (
                                   receipt_id         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                   invoice_id         UNIQUEIDENTIFIER NOT NULL,
                                   receipt_no         NVARCHAR(64)     NOT NULL,
                                   received_date      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                   method_id          UNIQUEIDENTIFIER NOT NULL,
                                   amount_total       DECIMAL(18,2)    NOT NULL,
                                   note               NVARCHAR(1000)   NULL,
                                   created_at         DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                   created_by         UNIQUEIDENTIFIER NOT NULL,

                                   CONSTRAINT PK_receipts PRIMARY KEY (receipt_id),
                                   CONSTRAINT UQ_receipts_no UNIQUE (receipt_no),
                                   CONSTRAINT UQ_receipts_invoice UNIQUE (invoice_id), -- 1 invoice ↔ tối đa 1 receipt
                                   CONSTRAINT FK_receipts_invoice FOREIGN KEY (invoice_id)
                                       REFERENCES [{{SCHEMA}}].invoices(invoice_id) ON DELETE CASCADE,
                                   CONSTRAINT FK_receipts_method FOREIGN KEY (method_id)
                                       REFERENCES [{{SCHEMA}}].payment_methods(payment_method_id),
                                   CONSTRAINT FK_receipts_created_by FOREIGN KEY (created_by)
                                       REFERENCES [{{SCHEMA}}].users(user_id)
);
GO

GO

/* ============================================================================
   8) ANNOUNCEMENTS
============================================================================ */
CREATE TABLE [{{SCHEMA}}].announcements (
                                        announcement_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                        title           NVARCHAR(255)    NOT NULL,
                                        content         NVARCHAR(MAX)    NOT NULL,
                                        visible_from    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                        visible_to      DATETIME2(3)     NULL,
                                        visibility_scope NVARCHAR(255),
                                        status          NVARCHAR(32)     NOT NULL DEFAULT N'PUBLISHED',
                                        is_pinned       BIT NOT NULL DEFAULT 0,
                                        type            NVARCHAR(50) NULL,
                                        created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                        created_by      NVARCHAR(190)    NULL,
                                        updated_at      DATETIME2(3)     NULL,
                                        updated_by      NVARCHAR(190)    NULL,
                                        schedule_id     UNIQUEIDENTIFIER NULL,
                                        booking_id      UNIQUEIDENTIFIER NULL,
                                        CONSTRAINT PK_announcements PRIMARY KEY (announcement_id),
                                        CONSTRAINT FK_ann_schedule FOREIGN KEY (schedule_id) REFERENCES [{{SCHEMA}}].asset_maintenance_schedule(schedule_id),
                                        CONSTRAINT FK_ann_booking FOREIGN KEY (booking_id) REFERENCES [{{SCHEMA}}].amenity_bookings(booking_id)
);
GO

CREATE TABLE [{{SCHEMA}}].announcement_reads (
                                             announcement_read_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                             announcement_id      UNIQUEIDENTIFIER NOT NULL,
                                             user_id         UNIQUEIDENTIFIER NULL,
                                             read_at              DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                             CONSTRAINT PK_announcement_reads PRIMARY KEY (announcement_read_id),
                                             CONSTRAINT UQ_announcement_reads UNIQUE (announcement_id),
                                             CONSTRAINT FK_ar_announcement FOREIGN KEY (announcement_id)
                                                 REFERENCES [{{SCHEMA}}].announcements(announcement_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_ar_user_read FOREIGN KEY (user_id)
                                                 REFERENCES [{{SCHEMA}}].users(user_id) ON DELETE CASCADE
);
GO

/* ============================================================================
   9) DOCUMENTS / FILES / VERSIONING / ACTION LOG
============================================================================ */
CREATE TABLE [{{SCHEMA}}].files (
                                file_id      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                original_name NVARCHAR(255)    NOT NULL,
                                mime_type    NVARCHAR(128)    NOT NULL,
                                storage_path NVARCHAR(1000)   NOT NULL,
                                uploaded_by  NVARCHAR(190)    NULL,
                                uploaded_at  DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                CONSTRAINT PK_files PRIMARY KEY (file_id)
);
GO

CREATE TABLE [{{SCHEMA}}].documents (
                                    document_id  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                    category     NVARCHAR(64)     NOT NULL,
                                    title        NVARCHAR(255)    NOT NULL,
                                    visibility_scope NVARCHAR(120),
                                    status       NVARCHAR(32)     NOT NULL DEFAULT N'ACTIVE',
                                    current_version INT NULL,
                                    created_at   DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                    created_by   NVARCHAR(190)    NULL,
                                    CONSTRAINT PK_documents PRIMARY KEY (document_id)
);
GO

CREATE TABLE [{{SCHEMA}}].document_versions (
                                            document_version_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                            document_id         UNIQUEIDENTIFIER NOT NULL,
                                            version_no          INT              NOT NULL,
                                            file_id             UNIQUEIDENTIFIER NOT NULL,
                                            note                NVARCHAR(500)    NULL,
                                            changed_at          DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                            created_by          NVARCHAR(190)    NULL,
                                            CONSTRAINT PK_document_versions PRIMARY KEY (document_version_id),
                                            CONSTRAINT UQ_document_versions UNIQUE (document_id, version_no),
                                            CONSTRAINT FK_dv_document FOREIGN KEY (document_id)
                                                REFERENCES [{{SCHEMA}}].documents(document_id) ON DELETE CASCADE,
                                            CONSTRAINT FK_dv_file FOREIGN KEY (file_id)
                                                REFERENCES [{{SCHEMA}}].files(file_id)
);
GO

CREATE TABLE [{{SCHEMA}}].document_action_log (
                                              action_log_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                              document_id   UNIQUEIDENTIFIER NOT NULL,
                                              action        NVARCHAR(64)     NOT NULL,
                                              actor_id      UNIQUEIDENTIFIER NULL,
                                              action_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                              detail        NVARCHAR(1000)   NULL,
                                              CONSTRAINT PK_document_action_log PRIMARY KEY (action_log_id),
                                              CONSTRAINT FK_dal_document FOREIGN KEY (document_id)
                                                  REFERENCES [{{SCHEMA}}].documents(document_id) ON DELETE CASCADE
);
GO

IF COL_LENGTH('[{{SCHEMA}}].tickets', 'apartment_id') IS NULL
ALTER TABLE [{{SCHEMA}}].tickets ADD apartment_id UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('[{{SCHEMA}}].tickets', 'scope') IS NULL
ALTER TABLE [{{SCHEMA}}].tickets ADD scope NVARCHAR(32) NULL;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_tickets_apartment'
)
ALTER TABLE [{{SCHEMA}}].tickets
    ADD CONSTRAINT FK_tickets_apartment FOREIGN KEY (apartment_id)
        REFERENCES [{{SCHEMA}}].apartments(apartment_id);
GO
IF OBJECT_ID('[{{SCHEMA}}].ticket_attachments', 'U') IS NULL
CREATE TABLE [{{SCHEMA}}].ticket_attachments (
                                             attachment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                             ticket_id UNIQUEIDENTIFIER NOT NULL,
                                             file_id UNIQUEIDENTIFIER NOT NULL,
                                             note NVARCHAR(500) NULL,
                                             uploaded_by NVARCHAR(190) NULL,
                                             uploaded_at DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                             CONSTRAINT PK_ticket_attachments PRIMARY KEY (attachment_id),
                                             CONSTRAINT FK_ticket_attachments_ticket FOREIGN KEY (ticket_id)
                                                 REFERENCES [{{SCHEMA}}].tickets(ticket_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_ticket_attachments_file FOREIGN KEY (file_id)
                                                 REFERENCES [{{SCHEMA}}].files(file_id)
);
GO

IF COL_LENGTH('[{{SCHEMA}}].invoice_details', 'ticket_id') IS NULL
ALTER TABLE [{{SCHEMA}}].invoice_details ADD ticket_id UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_invoice_details_ticket'
)
ALTER TABLE [{{SCHEMA}}].invoice_details
    ADD CONSTRAINT FK_invoice_details_ticket FOREIGN KEY (ticket_id)
        REFERENCES [{{SCHEMA}}].tickets(ticket_id);
GO

-- Thêm cột ticket_id nếu chưa có
IF COL_LENGTH('[{{SCHEMA}}].voucher_items', 'ticket_id') IS NULL
ALTER TABLE [{{SCHEMA}}].voucher_items ADD ticket_id UNIQUEIDENTIFIER NULL;

-- Thêm ràng buộc khóa ngoại
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_voucher_items_ticket'
)
ALTER TABLE [{{SCHEMA}}].voucher_items
    ADD CONSTRAINT FK_voucher_items_ticket FOREIGN KEY (ticket_id)
        REFERENCES [{{SCHEMA}}].tickets(ticket_id);
GO

IF OBJECT_ID('[{{SCHEMA}}].service_type_categories', 'U') IS NULL
    BEGIN
        CREATE TABLE [{{SCHEMA}}].service_type_categories
        (
            category_id   UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_stc_category_id DEFAULT NEWID(),
            [name]        NVARCHAR(100)    NOT NULL,
            [description] NVARCHAR(255)    NULL,
            created_at    DATETIME2(3)     NOT NULL
                CONSTRAINT DF_stc_created_at DEFAULT SYSDATETIME(),
            CONSTRAINT PK_stc PRIMARY KEY (category_id),
            CONSTRAINT UQ_stc_name UNIQUE ([name])
        );
    END
GO

MERGE [{{SCHEMA}}].service_type_categories AS tgt
USING (VALUES
           (N'Utility',     N'Điện, nước, rác, internet...'),
           (N'Service',     N'Dịch vụ bổ sung'),
           (N'Fee',         N'Phí một lần / hành chính'),
           (N'Maintenance', N'Bảo trì, sửa chữa'),
           (N'Other',       N'Khác')
) AS src([name],[description])
ON tgt.[name] = src.[name]
WHEN NOT MATCHED THEN
    INSERT([name],[description]) VALUES (src.[name], src.[description]);
GO

IF COL_LENGTH('[{{SCHEMA}}].service_types', 'category_id') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].service_types
            ADD category_id UNIQUEIDENTIFIER NULL; -- tạm NULL để migrate dữ liệu
    END
GO

-- Bước 1: tạo thêm các category mới nếu có giá trị lạ trong cột 'category'
INSERT INTO [{{SCHEMA}}].service_type_categories([name],[description])
SELECT DISTINCT st.[category], N'Imported from service_types'
FROM [{{SCHEMA}}].service_types st
         LEFT JOIN [{{SCHEMA}}].service_type_categories stc ON st.[category] = stc.[name]
WHERE st.[category] IS NOT NULL
  AND LTRIM(RTRIM(st.[category])) <> ''
  AND stc.category_id IS NULL;

-- Bước 2: cập nhật category_id dựa trên tên
UPDATE st
SET st.category_id = stc.category_id
FROM [{{SCHEMA}}].service_types st
         JOIN [{{SCHEMA}}].service_type_categories stc
              ON st.[category] = stc.[name]
WHERE st.category_id IS NULL;
GO

-- Kiểm tra còn hàng nào chưa có category_id
IF EXISTS (SELECT 1 FROM [{{SCHEMA}}].service_types WHERE category_id IS NULL)
    BEGIN
        RAISERROR('There are service_types rows with NULL category_id. Please fix mapping before setting NOT NULL.', 16, 1);
    END
ELSE
    BEGIN
        ALTER TABLE [{{SCHEMA}}].service_types
            ALTER COLUMN category_id UNIQUEIDENTIFIER NOT NULL;

        -- Thêm FK (đặt tên rõ ràng, tạo nếu chưa có)
        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = 'FK_service_types_category'
              AND parent_object_id = OBJECT_ID('[{{SCHEMA}}].service_types')
        )
            BEGIN
                ALTER TABLE [{{SCHEMA}}].service_types
                    ADD CONSTRAINT FK_service_types_category
                        FOREIGN KEY (category_id)
                            REFERENCES [{{SCHEMA}}].service_type_categories(category_id);
            END

        -- Index hỗ trợ lọc/joins
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = 'IX_service_types_category'
              AND object_id = OBJECT_ID('[{{SCHEMA}}].service_types')
        )
            BEGIN
                CREATE INDEX IX_service_types_category
                    ON [{{SCHEMA}}].service_types(category_id);
            END
    END
GO

IF COL_LENGTH('[{{SCHEMA}}].service_types', 'category') IS NOT NULL
    BEGIN
        -- Nếu có default/constraint/index gắn với [category] thì drop trước (đổi tên cho đúng DB thực tế)
        -- Ví dụ:
        -- ALTER TABLE [{{SCHEMA}}].service_types DROP CONSTRAINT DF_service_types_category;
        -- DROP INDEX IX_service_types_category_str ON [{{SCHEMA}}].service_types;

        ALTER TABLE [{{SCHEMA}}].service_types
            DROP COLUMN [category];
    END
GO


SET XACT_ABORT ON;
BEGIN TRAN;

------------------------------------------------------------
-- 1) Tạo bảng vai trò (nếu chưa có)
------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[{{SCHEMA}}].work_roles') AND type = 'U'
)
    BEGIN
        CREATE TABLE [{{SCHEMA}}].work_roles (
                                             role_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                                             role_key  NVARCHAR(100)    NOT NULL,
                                             role_name NVARCHAR(200)    NOT NULL,
                                             is_active BIT              NOT NULL DEFAULT 1,
                                             created_at DATETIME2(3)    NOT NULL DEFAULT SYSDATETIME(),
                                             updated_at DATETIME2(3)    NULL,
                                             CONSTRAINT PK_work_roles PRIMARY KEY (role_id),
                                             CONSTRAINT UQ_work_roles_key UNIQUE (role_key)
        );
    END

-- building_management
IF NOT EXISTS (
    SELECT 1 FROM [{{SCHEMA}}].work_roles WHERE role_key = N'building_management'
)
INSERT INTO [{{SCHEMA}}].work_roles (role_id, role_key, role_name, is_active)
VALUES ('0566433E-F36B-1410-82FA-00A94C248FA9', N'building_management', N'Quản trị vận hành tòa', 1);


-- accounting
IF NOT EXISTS (
    SELECT 1 FROM [{{SCHEMA}}].work_roles WHERE role_key = N'accounting'
)
INSERT INTO [{{SCHEMA}}].work_roles (role_id, role_key, role_name, is_active)
VALUES ('0666433E-F36B-1410-82FA-00A94C248FA9', N'accounting', N'Kế toán tòa nhà', 1);


-- reception
IF NOT EXISTS (
    SELECT 1 FROM [{{SCHEMA}}].work_roles WHERE role_key = N'reception'
)
INSERT INTO [{{SCHEMA}}].work_roles (role_id, role_key, role_name, is_active)
VALUES ('0766433E-F36B-1410-82FA-00A94C248FA9', N'reception', N'Lễ tân', 1);


-- resident_management
IF NOT EXISTS (
    SELECT 1 FROM [{{SCHEMA}}].work_roles WHERE role_key = N'resident_management'
)
INSERT INTO [{{SCHEMA}}].work_roles (role_id, role_key, role_name, is_active)
VALUES ('0866433E-F36B-1410-82FA-00A94C248FA9', N'resident_management', N'Quản lý cư dân', 1);

-- uniqueidentifier/DEFAULT: tài liệu MS SQL.
-- FOREIGN KEY sẽ thêm ở bước sau.  -- :contentReference[oaicite:1]{index=1}

------------------------------------------------------------
-- 2) Seed 4 vai trò (idempotent)
------------------------------------------------------------
MERGE [{{SCHEMA}}].work_roles AS t
USING (VALUES
           (N'[{{SCHEMA}}]_management',  N'Quản trị vận hành tòa'),
           (N'accounting',           N'Kế toán tòa nhà'),
           (N'reception',            N'Lễ tân'),
           (N'resident_management',  N'Quản lý cư dân')
) AS s(role_key, role_name)
ON t.role_key = s.role_key
WHEN NOT MATCHED THEN
    INSERT (role_key, role_name) VALUES (s.role_key, s.role_name);
-- MERGE theo tài liệu MS.  -- :contentReference[oaicite:2]{index=2}

------------------------------------------------------------
-- 3) Thêm cột role_id vào staff_profiles (nullable để backfill)
------------------------------------------------------------
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles','role_id') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD role_id UNIQUEIDENTIFIER NULL;  -- chỉ thêm cột, chưa NOT NULL
    END
-- Kiểm tra tồn tại cột bằng COL_LENGTH.  -- :contentReference[oaicite:3]{index=3}

------------------------------------------------------------
-- 4) (Tuỳ chọn) Map tường minh theo staff_code qua bảng tạm
------------------------------------------------------------
IF OBJECT_ID('tempdb..#staff_role_map') IS NOT NULL DROP TABLE #staff_role_map;
CREATE TABLE #staff_role_map (
                                 staff_code NVARCHAR(50) NOT NULL,
                                 role_key   NVARCHAR(100) NOT NULL
);

/* Điền mapping thật nếu có, ví dụ:
INSERT INTO #staff_role_map(staff_code, role_key) VALUES
 (N'0F2F3B3E-....', N'reception');  -- staff_code của bạn là GUID
*/

-- Map theo bảng tạm (nếu có dữ liệu)
UPDATE s
SET s.role_id = r.role_id
FROM [{{SCHEMA}}].staff_profiles s
         JOIN #staff_role_map m ON m.staff_code = CONVERT(NVARCHAR(50), s.staff_code)
         JOIN [{{SCHEMA}}].work_roles r ON r.role_key = m.role_key;

------------------------------------------------------------
-- 5) Fallback an toàn: gán 'reception' cho những dòng chưa có role_id
--    (để script chạy trọn vẹn; bạn có thể sửa lại sau)
------------------------------------------------------------
UPDATE s
SET s.role_id = r.role_id
FROM [{{SCHEMA}}].staff_profiles s
         CROSS JOIN [{{SCHEMA}}].work_roles r
WHERE s.role_id IS NULL
  AND r.role_key = N'reception';

------------------------------------------------------------
-- 6) Khóa NOT NULL + tạo FOREIGN KEY + INDEX
------------------------------------------------------------
ALTER TABLE [{{SCHEMA}}].staff_profiles
    ALTER COLUMN role_id UNIQUEIDENTIFIER NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_staff_profiles_role'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA}}].staff_profiles')
)
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD CONSTRAINT FK_staff_profiles_role
                FOREIGN KEY (role_id) REFERENCES [{{SCHEMA}}].work_roles(role_id);
    END
-- Cách tạo FOREIGN KEY theo tài liệu MS.  -- :contentReference[oaicite:4]{index=4}

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_staff_profiles_role'
      AND object_id = OBJECT_ID(N'[{{SCHEMA}}].staff_profiles')
)
    BEGIN
        CREATE INDEX IX_staff_profiles_role ON [{{SCHEMA}}].staff_profiles(role_id);
    END

COMMIT TRAN;

SET XACT_ABORT ON;
BEGIN TRAN;

-- is_active (NOT NULL, default 1)
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'is_active') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD is_active BIT NOT NULL
                CONSTRAINT DF_staff_profiles_is_active DEFAULT (1) WITH VALUES;
    END

-- current_address
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'current_address') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD current_address NVARCHAR(250) NULL;
    END

-- emergency_contact_name
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'emergency_contact_name') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD emergency_contact_name NVARCHAR(150) NULL;
    END

-- emergency_contact_phone
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'emergency_contact_phone') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD emergency_contact_phone NVARCHAR(20) NULL;
    END

-- emergency_contact_relation
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'emergency_contact_relation') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD emergency_contact_relation NVARCHAR(50) NULL;
    END

-- bank_account_no
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'bank_account_no') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD bank_account_no NVARCHAR(50) NULL;
    END

-- bank_name
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'bank_name') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD bank_name NVARCHAR(100) NULL;
    END

-- base_salary
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'base_salary') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD base_salary DECIMAL(18,2) NULL;
    END

-- tax_code
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'tax_code') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD tax_code NVARCHAR(50) NULL;
    END

-- social_insurance_no
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'social_insurance_no') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD social_insurance_no NVARCHAR(50) NULL;
    END

-- card_photo_url
IF COL_LENGTH('[{{SCHEMA}}].staff_profiles', 'card_photo_url') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].staff_profiles
            ADD card_photo_url NVARCHAR(300) NULL;
    END

COMMIT TRAN;

-- add columns if not exists
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','entry_type') IS NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries ADD entry_type NVARCHAR(50) NULL;
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','fiscal_period') IS NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries ADD fiscal_period NVARCHAR(20) NULL;

-- drop check constraint if exists
IF EXISTS (
    SELECT 1 FROM sys.check_constraints cc
    JOIN sys.objects o ON cc.parent_object_id = o.object_id
    JOIN sys.schemas s ON o.schema_id = s.schema_id
    WHERE cc.name = N'CK_je_totals_nonneg' AND s.name = N'{{SCHEMA}}' AND o.name = N'journal_entries'
)
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP CONSTRAINT CK_je_totals_nonneg;

-- drop foreign key if exists
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.objects o ON fk.parent_object_id = o.object_id
    JOIN sys.schemas s ON o.schema_id = s.schema_id
    WHERE fk.name = N'FK_je_reversed_by' AND s.name = N'{{SCHEMA}}' AND o.name = N'journal_entries'
)
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP CONSTRAINT FK_je_reversed_by;

-- drop any default constraints bound to the columns we're removing, then drop the columns
DECLARE @sql NVARCHAR(MAX);

-- total_debit & total_credit defaults
SELECT @sql = STRING_AGG('ALTER TABLE [' + s.name + '].[' + o.name + '] DROP CONSTRAINT [' + dc.name + '];', CHAR(13)+CHAR(10))
FROM sys.default_constraints dc
JOIN sys.objects o ON dc.parent_object_id = o.object_id
JOIN sys.schemas s ON o.schema_id = s.schema_id
JOIN sys.columns c ON c.object_id = o.object_id AND c.column_id = dc.parent_column_id
WHERE s.name = N'{{SCHEMA}}' AND o.name = N'journal_entries' AND c.name IN (N'total_debit', N'total_credit');

IF @sql IS NOT NULL EXEC sp_executesql @sql;

-- now drop the columns if they exist
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','total_debit') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP COLUMN total_debit;
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','total_credit') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP COLUMN total_credit;
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','reversed_by') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP COLUMN reversed_by;
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','reversed_date') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP COLUMN reversed_date;
IF COL_LENGTH('[{{SCHEMA}}].journal_entries','reversal_entry_id') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entries DROP COLUMN reversal_entry_id;

-- journal_entry_lines columns
IF COL_LENGTH('[{{SCHEMA}}].journal_entry_lines','department') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entry_lines DROP COLUMN department;
IF COL_LENGTH('[{{SCHEMA}}].journal_entry_lines','cost_center') IS NOT NULL
    ALTER TABLE [{{SCHEMA}}].journal_entry_lines DROP COLUMN cost_center;

/* ============================================================================
   10) INDEXES (performance hints)
============================================================================ */
CREATE INDEX IX_resident_apartments_apartment ON [{{SCHEMA}}].resident_apartments(apartment_id);
CREATE INDEX IX_vehicles_resident ON [{{SCHEMA}}].vehicles(resident_id);
CREATE INDEX IX_parking_entries_card_time ON [{{SCHEMA}}].parking_entries(card_id, entry_time);
CREATE INDEX IX_meter_readings_time ON [{{SCHEMA}}].meter_readings(meter_id, reading_time);
CREATE INDEX IX_invoices_apartment_due ON [{{SCHEMA}}].invoices(apartment_id, due_date);
CREATE INDEX IX_tickets_status_priority ON [{{SCHEMA}}].tickets(status, priority, created_at);
CREATE INDEX IX_amenity_bookings_amenity_time ON [{{SCHEMA}}].amenity_bookings(amenity_id, start_date, end_date);
CREATE INDEX IX_announcements_booking_id ON [{{SCHEMA}}].announcements(booking_id);
GO
ALTER TABLE [{{SCHEMA}}].[floors]
ADD
    floor_type NVARCHAR(50),
    created_at DATETIME2 ,
    updated_at DATETIME2 ;
GO
ALTER TABLE [{{SCHEMA}}].[Tickets]
ADD vehicle_id UNIQUEIDENTIFIER NULL;
ALTER TABLE [{{SCHEMA}}].[Tickets]
ADD CONSTRAINT FK_Ticket_Vehicle
FOREIGN KEY (vehicle_id)
REFERENCES [{{SCHEMA}}].[Vehicles](vehicle_id);

ALTER TABLE [{{SCHEMA}}].[service_types]
ADD is_delete BIT NULL;

ALTER TABLE [{{SCHEMA}}].[resident_profiles]
ADD
    is_verified    BIT            NOT NULL CONSTRAINT DF_resident_profiles_is_verified DEFAULT (0),
    verified_at    DATETIME2(3)   NULL,
    nationality    NVARCHAR(64)   NULL,
    internal_note  NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[{{SCHEMA}}].[invoice_configurations]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [{{SCHEMA}}].[invoice_configurations] (
        config_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        generation_day_of_month INT NOT NULL DEFAULT 1 CHECK (generation_day_of_month BETWEEN 1 AND 28),
        due_days_after_issue INT NOT NULL DEFAULT 40 CHECK (due_days_after_issue BETWEEN 1 AND 90),
        is_enabled BIT NOT NULL DEFAULT 1,
        notes NVARCHAR(500),
        created_at DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        created_by NVARCHAR(190),
        updated_at DATETIME2(3),
        updated_by NVARCHAR(190)
    );
END

IF COL_LENGTH('[{{SCHEMA}}].documents', 'is_delete') IS NULL
    BEGIN
        ALTER TABLE [{{SCHEMA}}].documents
            ADD is_delete BIT NOT NULL CONSTRAINT DF_documents_is_delete DEFAULT 0;
    END

INSERT INTO [{{SCHEMA}}].vehicle_types (code, name)
VALUES
    (N'CAR',       N'ô tô'),
    (N'BICYCLE',   N'Xe đạp'),
    (N'MOTORBIKE', N'Xe máy');

