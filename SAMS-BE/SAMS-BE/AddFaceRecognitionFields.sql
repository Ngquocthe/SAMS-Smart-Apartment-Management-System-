-- Migration script để thêm các field cho Face Recognition
-- Chạy script này trên database để thêm các cột cần thiết

USE [YourDatabaseName]; -- Thay đổi tên database của bạn
GO

-- 1. Thêm cột face_embedding vào bảng users
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('building.users') 
    AND name = 'face_embedding'
)
BEGIN
    ALTER TABLE building.users
    ADD face_embedding VARBINARY(MAX) NULL;
    
    PRINT 'Added face_embedding column to building.users';
END
ELSE
BEGIN
    PRINT 'Column face_embedding already exists in building.users';
END
GO

-- 2. Thêm cột avatar_url vào bảng users (để lưu URL ảnh khuôn mặt)
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('building.users')
    AND name = 'avatar_url'
)
BEGIN
    ALTER TABLE building.users
    ADD avatar_url NVARCHAR(300) NULL;

    PRINT 'Added avatar_url column to building.users';
END
ELSE
BEGIN
    PRINT 'Column avatar_url already exists in building.users';
END
GO

-- 3. Thêm cột requires_face_verification vào bảng amenities
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('building.amenities') 
    AND name = 'requires_face_verification'
)
BEGIN
    ALTER TABLE building.amenities
    ADD requires_face_verification BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added requires_face_verification column to building.amenities';
END
ELSE
BEGIN
    PRINT 'Column requires_face_verification already exists in building.amenities';
END
GO

-- 4. Tạo index cho face_embedding (nếu cần tối ưu query)
-- Lưu ý: VARBINARY(MAX) không thể tạo index trực tiếp, nhưng có thể tạo computed column nếu cần

PRINT 'Migration completed successfully!';
GO

-- 5. Tạo bảng để lưu lịch sử check-in bằng khuôn mặt
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'amenity_check_ins'
      AND s.name = 'building'
)
BEGIN
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
            FOREIGN KEY (booking_id) REFERENCES building.amenity_bookings(booking_id),
        CONSTRAINT FK_amenity_check_ins_checked_for
            FOREIGN KEY (checked_in_for_user_id) REFERENCES building.users(user_id),
        CONSTRAINT FK_amenity_check_ins_checked_by
            FOREIGN KEY (checked_in_by_user_id) REFERENCES building.users(user_id)
    );

    CREATE INDEX IX_amenity_check_ins_booking
        ON building.amenity_check_ins (booking_id);

    CREATE INDEX IX_amenity_check_ins_checked_in_at
        ON building.amenity_check_ins (checked_in_at DESC);

    PRINT 'Created building.amenity_check_ins table';
END
ELSE
BEGIN
    PRINT 'Table building.amenity_check_ins already exists';
END
GO


