/* ============================================================================
   1) LOOKUP / DICTIONARIES
============================================================================ */

CREATE TABLE building.users(
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
);

CREATE TABLE building.work_roles (
    role_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    role_key  NVARCHAR(100)    NOT NULL,
    role_name NVARCHAR(200)    NOT NULL,
    is_active BIT              NOT NULL DEFAULT 1,
    created_at DATETIME2(3)    NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2(3)    NULL,
    CONSTRAINT PK_work_roles PRIMARY KEY (role_id),
    CONSTRAINT UQ_work_roles_key UNIQUE (role_key)
);
GO

MERGE building.work_roles AS t
USING (VALUES
    (N'building_management',N'Quản trị vận hành tòa'),
    (N'accounting',N'Kế toán tòa nhà'),
    (N'reception',N'Lễ tân'),
    (N'resident_management',N'Quản lý cư dân')
) AS s(role_key,role_name)
ON t.role_key=s.role_key
WHEN NOT MATCHED THEN INSERT(role_key,role_name) VALUES(s.role_key,s.role_name);
GO

CREATE TABLE building.staff_profiles (
    staff_code       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    user_id          UNIQUEIDENTIFIER NULL,
    hire_date        DATE             NULL,
    termination_date DATE             NULL,
    notes            NVARCHAR(1000)   NULL,
    is_active        BIT NOT NULL DEFAULT 1,
    current_address  NVARCHAR(250) NULL,
    emergency_contact_name NVARCHAR(150) NULL,
    emergency_contact_phone NVARCHAR(20) NULL,
    emergency_contact_relation NVARCHAR(50) NULL,
    bank_account_no  NVARCHAR(50) NULL,
    bank_name        NVARCHAR(100) NULL,
    base_salary      DECIMAL(18,2) NULL,
    tax_code         NVARCHAR(50) NULL,
    social_insurance_no NVARCHAR(50) NULL,
    card_photo_url   NVARCHAR(300) NULL,
    role_id          UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_staff_profiles PRIMARY KEY (staff_code),
    CONSTRAINT FK_staff_profiles_user FOREIGN KEY (user_id) REFERENCES building.users(user_id),
    CONSTRAINT FK_staff_profiles_role FOREIGN KEY (role_id) REFERENCES building.work_roles(role_id)
);
GO

CREATE TABLE building.asset_categories (
    category_id        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    code               NVARCHAR(64)     NOT NULL,
    name               NVARCHAR(255)    NOT NULL,
    description        NVARCHAR(1000)   NULL,
    maintenance_frequency INT NULL,
    default_reminder_days INT NULL DEFAULT 3,
    CONSTRAINT PK_asset_categories PRIMARY KEY (category_id),
    CONSTRAINT UQ_asset_categories_code UNIQUE (code)
);
GO

CREATE TABLE building.vehicle_types (
    vehicle_type_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    code            NVARCHAR(64)     NOT NULL,
    name            NVARCHAR(255)    NOT NULL,
    CONSTRAINT PK_vehicle_types PRIMARY KEY (vehicle_type_id),
    CONSTRAINT UQ_vehicle_types_code UNIQUE (code)
);
GO

CREATE TABLE building.payment_methods (
    payment_method_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    code              NVARCHAR(64)     NOT NULL,
    name              NVARCHAR(255)    NOT NULL,
    active            BIT              NOT NULL DEFAULT (1),
    CONSTRAINT PK_payment_methods PRIMARY KEY (payment_method_id),
    CONSTRAINT UQ_payment_methods_code UNIQUE (code)
);
GO

CREATE TABLE building.service_type_categories (
    category_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    name          NVARCHAR(100)    NOT NULL,
    description   NVARCHAR(255)    NULL,
    created_at    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_stc PRIMARY KEY (category_id),
    CONSTRAINT UQ_stc_name UNIQUE (name)
);
GO

MERGE building.service_type_categories AS tgt
USING (VALUES
    (N'Utility',N'Điện, nước, rác, internet...'),
    (N'Service',N'Dịch vụ bổ sung'),
    (N'Fee',N'Phí một lần / hành chính'),
    (N'Maintenance',N'Bảo trì, sửa chữa'),
    (N'Other',N'Khác')
) AS src(name,description)
ON tgt.name = src.name
WHEN NOT MATCHED THEN INSERT(name,description) VALUES(src.name,src.description);
GO

CREATE TABLE building.service_types (
    service_type_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    code            NVARCHAR(64)     NOT NULL,
    name            NVARCHAR(255)    NOT NULL,
    category_id     UNIQUEIDENTIFIER NOT NULL,
    unit            NVARCHAR(64)     NULL,
    is_mandatory    BIT              NOT NULL DEFAULT (0),
    is_recurring    BIT              NOT NULL DEFAULT (1),
    is_active       BIT              NOT NULL DEFAULT (1),
    is_delete       BIT              NULL,
    created_at      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    updated_at      DATETIME2(3)     NULL,
    CONSTRAINT PK_service_types PRIMARY KEY (service_type_id),
    CONSTRAINT UQ_service_types_code UNIQUE (code),
    CONSTRAINT FK_service_types_category FOREIGN KEY (category_id) REFERENCES building.service_type_categories(category_id)
);
GO

CREATE TABLE building.floors (
    floor_id     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    floor_number INT              NOT NULL,
    name         NVARCHAR(255)    NULL,
    CONSTRAINT PK_floors PRIMARY KEY (floor_id)
);
GO

CREATE TABLE building.apartments (
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
        REFERENCES building.floors(floor_id)
);
GO

CREATE TABLE building.resident_profiles (
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
        REFERENCES building.users(user_id)
);
GO

CREATE TABLE building.resident_apartments (
    resident_apartment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    resident_id           UNIQUEIDENTIFIER NOT NULL,
    apartment_id          UNIQUEIDENTIFIER NOT NULL,
    relation_type         NVARCHAR(32)     NOT NULL,
    start_date            DATE             NOT NULL,
    end_date              DATE             NULL,
    is_primary            BIT              NOT NULL DEFAULT (0),
    CONSTRAINT PK_resident_apartments PRIMARY KEY (resident_apartment_id),
    CONSTRAINT FK_ra_resident FOREIGN KEY (resident_id)
        REFERENCES building.resident_profiles(resident_id) ON DELETE CASCADE,
    CONSTRAINT FK_ra_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id) ON DELETE CASCADE,
    CONSTRAINT UQ_ra_unique UNIQUE (resident_id, apartment_id, relation_type, start_date)
);
GO

CREATE TABLE building.tickets (
    ticket_id      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    created_by_user_id UNIQUEIDENTIFIER NULL,
    category       NVARCHAR(64)     NOT NULL,
    priority       NVARCHAR(32)     NULL,
    subject        NVARCHAR(255)    NOT NULL,
    description    NVARCHAR(MAX)    NULL,
    status         NVARCHAR(32)     NOT NULL DEFAULT N'Mới tạo',
    expected_completion_at DATETIME2(3) NULL,
    created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    updated_at     DATETIME2(3)     NULL,
    closed_at      DATETIME2(3)     NULL,
    scope          NVARCHAR(64)     NULL,
    apartment_id   UNIQUEIDENTIFIER NULL,
    has_invoice    BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_tickets PRIMARY KEY (ticket_id),
    CONSTRAINT FK_tickets_users FOREIGN KEY (created_by_user_id)
        REFERENCES building.users(user_id),
    CONSTRAINT FK_tickets_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id)
);
GO

CREATE TABLE building.maintenance_apartment_history (
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
        REFERENCES building.apartments(apartment_id),
    CONSTRAINT FK_mah_creator FOREIGN KEY (creator_user_id)
        REFERENCES building.users(user_id),
    CONSTRAINT FK_mah_handler FOREIGN KEY (handler_user_id)
        REFERENCES building.users(user_id),
    CONSTRAINT FK_mah_request FOREIGN KEY (request_id)
        REFERENCES building.tickets(ticket_id),
    CONSTRAINT CK_mah_priority CHECK (priority IN (N'LOW',N'NORMAL',N'HIGH',N'URGENT')),
    CONSTRAINT CK_mah_status   CHECK (status IN (N'OPEN',N'IN_PROGRESS',N'RESOLVED',N'CLOSED',N'CANCELLED'))
);
GO

/* ============================================================================
   3) CARDS / VEHICLES / PARKING / ACCESS
============================================================================ */
CREATE TABLE building.access_cards (
    card_id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    card_number           NVARCHAR(128)    NOT NULL,       -- Mã số thẻ vật lý
    status                NVARCHAR(32)     NOT NULL DEFAULT N'PENDING_APPROVAL',
    issued_to_user_id     UNIQUEIDENTIFIER NULL,
    issued_to_apartment_id UNIQUEIDENTIFIER NULL,
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
        REFERENCES building.users(user_id),
    CONSTRAINT FK_access_cards_apartment FOREIGN KEY (issued_to_apartment_id)
        REFERENCES building.apartments(apartment_id)
);
GO

--Card
CREATE TABLE building.access_card_types (
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

CREATE TABLE building.access_card_capabilities (
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
                                                       REFERENCES building.access_cards(card_id) ON DELETE CASCADE,
                                                   CONSTRAINT FK_acc_type FOREIGN KEY (card_type_id)
                                                       REFERENCES building.access_card_types(card_type_id) ON DELETE CASCADE
);
GO

CREATE TABLE [building].[card_history] (
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
                                               REFERENCES [building].[access_cards] ([card_id]) ON DELETE CASCADE,

                                           CONSTRAINT [FK_card_history_card_type] FOREIGN KEY ([card_type_id])
                                               REFERENCES [building].[access_card_types] ([card_type_id]) ON DELETE SET NULL
);
GO

CREATE TABLE building.vehicles (
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
                                       REFERENCES building.resident_profiles(resident_id),
                                   CONSTRAINT FK_vehicles_apartment FOREIGN KEY (apartment_id)
                                       REFERENCES building.apartments(apartment_id),
                                   CONSTRAINT FK_vehicles_type FOREIGN KEY (vehicle_type_id)
                                       REFERENCES building.vehicle_types(vehicle_type_id),
                                   CONSTRAINT FK_vehicles_card FOREIGN KEY (parking_card_id)
                                       REFERENCES building.access_cards(card_id)
);
GO

CREATE TABLE building.parking_entries (
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
                                              REFERENCES building.access_cards(card_id),
                                          CONSTRAINT FK_parking_entries_vehicle FOREIGN KEY (vehicle_id)
                                              REFERENCES building.vehicles(vehicle_id)
);
GO

/* ============================================================================
   4) AMENITIES & BOOKINGS
============================================================================ */
CREATE TABLE building.amenities (
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
    CONSTRAINT FK_amenities_asset FOREIGN KEY (asset_id) REFERENCES building.assets(asset_id)
);
GO

CREATE TABLE building.amenity_packages (
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
    CONSTRAINT FK_ap_amenity FOREIGN KEY(amenity_id) REFERENCES building.amenities(amenity_id) ON DELETE CASCADE
);
GO

CREATE TABLE building.amenity_bookings (
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
        REFERENCES building.amenities(amenity_id),
    CONSTRAINT FK_ab_package FOREIGN KEY (package_id)
        REFERENCES building.amenity_packages(package_id),
    CONSTRAINT FK_ab_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id),
    CONSTRAINT FK_ab_user FOREIGN KEY (user_id)
        REFERENCES building.users(user_id)
);
GO

    CREATE TABLE building.amenity_check_ins
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
            FOREIGN KEY (booking_id) REFERENCES building.amenity_bookings(booking_id) ON DELETE CASCADE,
        CONSTRAINT FK_amenity_check_ins_checked_for
            FOREIGN KEY (checked_in_for_user_id) REFERENCES building.users(user_id) ON DELETE NO ACTION,
        CONSTRAINT FK_amenity_check_ins_checked_by
            FOREIGN KEY (checked_in_by_user_id) REFERENCES building.users(user_id)
    );
    GO

/* ============================================================================
   5) SERVICES, PRICES, SUBSCRIPTIONS, METERS & READINGS
============================================================================ */
CREATE TABLE building.service_prices (
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
                                             REFERENCES building.service_types(service_type_id),
                                         CONSTRAINT CK_fee_dates CHECK (end_date IS NULL OR end_date > effective_date),
                                         CONSTRAINT FK_fee_created_by  FOREIGN KEY (created_by)  REFERENCES building.staff_profiles(staff_code),
                                         CONSTRAINT FK_fee_approved_by FOREIGN KEY (approved_by) REFERENCES building.staff_profiles(staff_code)
);
GO

CREATE TABLE building.apartment_services (
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
                                                 REFERENCES building.apartments(apartment_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_as_service FOREIGN KEY (service_id)
                                                 REFERENCES building.service_types(service_type_id)
);
GO

CREATE TABLE building.meters (
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
                                     REFERENCES building.apartments(apartment_id) ON DELETE CASCADE,
                                 CONSTRAINT FK_meters_service FOREIGN KEY (service_id)
                                     REFERENCES building.service_types(service_type_id)
);
GO

CREATE TABLE building.meter_readings (
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
                                             REFERENCES building.meters(meter_id) ON DELETE CASCADE
);
GO

/* ============================================================================
   6) INVOICING & PAYMENTS
============================================================================ */
CREATE TABLE building.vouchers (
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
    CONSTRAINT FK_vouchers_created_by FOREIGN KEY (created_by)  REFERENCES building.staff_profiles(staff_code),
    CONSTRAINT FK_vouchers_approved_by FOREIGN KEY (approved_by) REFERENCES building.staff_profiles(staff_code),
    CONSTRAINT FK_vouchers_ticket FOREIGN KEY (ticket_id) REFERENCES building.tickets(ticket_id),
    CONSTRAINT FK_vouchers_history FOREIGN KEY (history_id) REFERENCES building.asset_maintenance_history(history_id)
);
GO

CREATE TABLE building.voucher_items (
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
        REFERENCES building.vouchers(voucher_id) ON DELETE CASCADE,
    CONSTRAINT FK_vi_service_type FOREIGN KEY (service_type_id)
        REFERENCES building.service_types(service_type_id),
    CONSTRAINT FK_vi_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id)
);
GO

CREATE TABLE building.journal_entries (
    entry_id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    entry_number      NVARCHAR(64)     NOT NULL,
    entry_type        NVARCHAR(50)     NULL,
    entry_date        DATE             NOT NULL,
    reference_type    NVARCHAR(32)     NULL,
    reference_id      UNIQUEIDENTIFIER NULL,
    description       NVARCHAR(1000)   NULL,
    status            NVARCHAR(16)     NOT NULL DEFAULT N'DRAFT',
    posted_by         UNIQUEIDENTIFIER NULL,
    posted_date       DATETIME2(3)     NULL,
    created_by        UNIQUEIDENTIFIER NULL,
    created_at        DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    fiscal_period     NVARCHAR(20)     NOT NULL DEFAULT N'',
    CONSTRAINT PK_journal_entries PRIMARY KEY (entry_id),
    CONSTRAINT UQ_journal_entries_number UNIQUE (entry_number),
    CONSTRAINT FK_je_created_by  FOREIGN KEY (created_by)  REFERENCES building.staff_profiles(staff_code),
    CONSTRAINT FK_je_posted_by   FOREIGN KEY (posted_by)   REFERENCES building.staff_profiles(staff_code)
);
GO

CREATE TABLE building.journal_entry_lines (
    line_id       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    entry_id      UNIQUEIDENTIFIER NOT NULL,
    line_number   INT              NOT NULL,
    account_code  NVARCHAR(64)     NOT NULL,
    description   NVARCHAR(500)    NULL,
    debit_amount  DECIMAL(18,2)    NULL,
    credit_amount DECIMAL(18,2)    NULL,
    apartment_id  UNIQUEIDENTIFIER NULL,
    created_at    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_journal_entry_lines PRIMARY KEY (line_id),
    CONSTRAINT FK_jel_entry FOREIGN KEY (entry_id)
        REFERENCES building.journal_entries(entry_id) ON DELETE CASCADE,
    CONSTRAINT UQ_jel_entry_line UNIQUE (entry_id, line_number),
    CONSTRAINT CK_jel_amounts CHECK (
        (debit_amount  IS NULL OR debit_amount  >= 0) AND
        (credit_amount IS NULL OR credit_amount >= 0) AND
        NOT (ISNULL(debit_amount,0)=0 AND ISNULL(credit_amount,0)=0)
    ),
    CONSTRAINT FK_jel_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id)
);
GO

CREATE TABLE building.invoices (
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
        REFERENCES building.apartments(apartment_id),
    CONSTRAINT FK_invoices_ticket FOREIGN KEY (ticket_id)
        REFERENCES building.tickets(ticket_id)
);
GO

CREATE TABLE building.invoice_details (
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
        REFERENCES building.invoices(invoice_id) ON DELETE CASCADE,
    CONSTRAINT FK_invoice_details_service FOREIGN KEY (service_id)
        REFERENCES building.service_types(service_type_id)
);
GO

CREATE TABLE building.receipts (
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
    CONSTRAINT UQ_receipts_invoice UNIQUE (invoice_id),
    CONSTRAINT FK_receipts_invoice FOREIGN KEY (invoice_id)
        REFERENCES building.invoices(invoice_id) ON DELETE CASCADE,
    CONSTRAINT FK_receipts_method FOREIGN KEY (method_id)
        REFERENCES building.payment_methods(payment_method_id),
    CONSTRAINT FK_receipts_created_by FOREIGN KEY (created_by)
        REFERENCES building.users(user_id)
);
GO

GO

/* ============================================================================
   7) TICKETS / MAINTENANCE / APPOINTMENTS / ACTION LOG
============================================================================ */

CREATE TABLE building.ticket_comments (
                                          comment_id   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                          ticket_id    UNIQUEIDENTIFIER NOT NULL,
                                          commented_by UNIQUEIDENTIFIER NULL,
                                          comment_time DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                          content      NVARCHAR(MAX)    NOT NULL,
                                          CONSTRAINT PK_ticket_comments PRIMARY KEY (comment_id),
                                          CONSTRAINT FK_ticket_comments_ticket FOREIGN KEY (ticket_id)
                                              REFERENCES building.tickets(ticket_id) ON DELETE CASCADE
);
GO

CREATE TABLE building.appointments (
                                       appointment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                       ticket_id      UNIQUEIDENTIFIER NULL,
                                       apartment_id   UNIQUEIDENTIFIER NOT NULL,
                                       start_at       DATETIME2(3)     NOT NULL,
                                       end_at         DATETIME2(3)     NOT NULL,
                                       location       NVARCHAR(255)    NULL,
                                       created_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                       CONSTRAINT PK_appointments PRIMARY KEY (appointment_id),
                                       CONSTRAINT FK_appointments_ticket FOREIGN KEY (ticket_id)
                                           REFERENCES building.tickets(ticket_id),
                                       CONSTRAINT FK_appointments_apartment FOREIGN KEY (apartment_id)
                                           REFERENCES building.apartments(apartment_id)
);
GO

--Asset
CREATE TABLE building.assets (
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
        REFERENCES building.asset_categories(category_id),
    CONSTRAINT FK_assets_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id)
);
GO

CREATE TABLE building.asset_maintenance_schedule (
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
    CONSTRAINT FK_ams_asset FOREIGN KEY (asset_id) REFERENCES building.assets(asset_id) ON DELETE CASCADE,
    CONSTRAINT FK_ams_created_by FOREIGN KEY (created_by) REFERENCES building.users(user_id),
    CONSTRAINT FK_ams_completed_by FOREIGN KEY (completed_by) REFERENCES building.users(user_id)
);
GO

CREATE TABLE building.asset_maintenance_history (
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
        REFERENCES building.assets(asset_id) ON DELETE CASCADE,
    CONSTRAINT FK_amh_schedule FOREIGN KEY (schedule_id)
        REFERENCES building.asset_maintenance_schedule(schedule_id),
    CONSTRAINT FK_amh_performed_by FOREIGN KEY (performed_by)
        REFERENCES building.users(user_id)
);
GO

/* ============================================================================
   8) ANNOUNCEMENTS
============================================================================ */
CREATE TABLE building.announcements (
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
    CONSTRAINT FK_ann_schedule FOREIGN KEY (schedule_id) REFERENCES building.asset_maintenance_schedule(schedule_id),
    CONSTRAINT FK_ann_booking FOREIGN KEY (booking_id) REFERENCES building.amenity_bookings(booking_id)
);
GO

CREATE TABLE building.announcement_reads (
                                             announcement_read_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                             announcement_id      UNIQUEIDENTIFIER NOT NULL,
                                             user_id         UNIQUEIDENTIFIER NULL,
                                             read_at              DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                             CONSTRAINT PK_announcement_reads PRIMARY KEY (announcement_read_id),
                                             CONSTRAINT UQ_announcement_reads UNIQUE (
                                                                                      announcement_id
                                                 ),
                                             CONSTRAINT FK_ar_announcement FOREIGN KEY (announcement_id)
                                                 REFERENCES building.announcements(announcement_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_ar_user_read FOREIGN KEY (user_id)
                                                 REFERENCES building.users(user_id) ON DELETE CASCADE,
);
GO

/* ============================================================================
   9) DOCUMENTS / FILES / VERSIONING / ACTION LOG
============================================================================ */
CREATE TABLE building.files (
                                file_id      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                original_name NVARCHAR(255)    NOT NULL,
                                mime_type    NVARCHAR(128)    NOT NULL,
                                storage_path NVARCHAR(1000)   NOT NULL,
                                uploaded_by  NVARCHAR(190)    NULL,
                                uploaded_at  DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                CONSTRAINT PK_files PRIMARY KEY (file_id)
);
GO

CREATE TABLE building.documents (
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

CREATE TABLE building.document_versions (
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
                                                REFERENCES building.documents(document_id) ON DELETE CASCADE,
                                            CONSTRAINT FK_dv_file FOREIGN KEY (file_id)
                                                REFERENCES building.files(file_id)
);
GO

CREATE TABLE building.document_action_log (
                                              action_log_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                              document_id   UNIQUEIDENTIFIER NOT NULL,
                                              action        NVARCHAR(64)     NOT NULL,
                                              actor_id      UNIQUEIDENTIFIER NULL,
                                              action_at     DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
                                              detail        NVARCHAR(1000)   NULL,
                                              CONSTRAINT PK_document_action_log PRIMARY KEY (action_log_id),
                                              CONSTRAINT FK_dal_document FOREIGN KEY (document_id)
                                                  REFERENCES building.documents(document_id) ON DELETE CASCADE
);
GO

IF COL_LENGTH('building.tickets', 'apartment_id') IS NULL
ALTER TABLE building.tickets ADD apartment_id UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('building.tickets', 'scope') IS NULL
ALTER TABLE building.tickets ADD scope NVARCHAR(32) NULL;

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys WHERE name = 'FK_tickets_apartment'
)
ALTER TABLE building.tickets
    ADD CONSTRAINT FK_tickets_apartment FOREIGN KEY (apartment_id)
        REFERENCES building.apartments(apartment_id);
GO
IF OBJECT_ID('building.ticket_attachments', 'U') IS NULL
CREATE TABLE building.ticket_attachments (
                                             attachment_id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                                             ticket_id UNIQUEIDENTIFIER NOT NULL,
                                             file_id UNIQUEIDENTIFIER NOT NULL,
                                             note NVARCHAR(500) NULL,
                                             uploaded_by NVARCHAR(190) NULL,
                                             uploaded_at DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
                                             CONSTRAINT PK_ticket_attachments PRIMARY KEY (attachment_id),
                                             CONSTRAINT FK_ticket_attachments_ticket FOREIGN KEY (ticket_id)
                                                 REFERENCES building.tickets(ticket_id) ON DELETE CASCADE,
                                             CONSTRAINT FK_ticket_attachments_file FOREIGN KEY (file_id)
                                                 REFERENCES building.files(file_id)
);
GO

IF COL_LENGTH('building.invoice_details', 'ticket_id') IS NULL
ALTER TABLE building.invoice_details ADD ticket_id UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_invoice_details_ticket'
)
ALTER TABLE building.invoice_details
    ADD CONSTRAINT FK_invoice_details_ticket FOREIGN KEY (ticket_id)
        REFERENCES building.tickets(ticket_id);
GO

-- Thêm cột ticket_id nếu chưa có

/* ============================================================================
   10) INDEXES (performance hints)
============================================================================ */
CREATE INDEX IX_resident_apartments_apartment ON building.resident_apartments(apartment_id);
CREATE INDEX IX_vehicles_resident ON building.vehicles(resident_id);
CREATE INDEX IX_parking_entries_card_time ON building.parking_entries(card_id, entry_time);
CREATE INDEX IX_meter_readings_time ON building.meter_readings(meter_id, reading_time);
CREATE INDEX IX_invoices_apartment_due ON building.invoices(apartment_id, due_date);
CREATE INDEX IX_tickets_status_priority ON building.tickets(status, priority, created_at);
CREATE INDEX IX_amenity_bookings_amenity_time ON building.amenity_bookings(amenity_id, start_date, end_date);
GO
