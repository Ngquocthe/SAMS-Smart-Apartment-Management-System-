using Microsoft.AspNetCore.Http;

namespace SAMS_BE.Helpers
{
    public static class FileValidationHelper
    {
        // File size limits (in bytes)
        public const long MAX_FILE_SIZE_TICKET = 100 * 1024 * 1024; // 100MB for tickets
        public const long MAX_FILE_SIZE_DOCUMENT = 100 * 1024 * 1024; // 100MB for documents
        public const long MAX_FILE_SIZE_GENERAL = 50 * 1024 * 1024; // 50MB default

        // Allowed file extensions
        public static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };

        public static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
        };

        public static readonly HashSet<string> AllowedTicketExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", // Images
            ".pdf", ".doc", ".docx" // Documents
        };

        // Dangerous file extensions (blocked for security)
        public static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".com", ".scr", ".vbs", ".js", ".jar", ".msi", ".dll",
            ".sh", ".ps1", ".psm1", ".psd1", ".app", ".deb", ".rpm", ".dmg", ".pkg",
            ".php", ".asp", ".aspx", ".jsp", ".py", ".rb", ".pl", ".cgi"
        };

        // Allowed MIME types
        public static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp"
        };

        public static readonly HashSet<string> AllowedDocumentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain",
            "text/csv"
        };

        public static readonly HashSet<string> AllowedTicketMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        /// <summary>
        /// Validates file for ticket attachments
        /// </summary>
        public static void ValidateTicketFile(IFormFile file)
        {
            if (file == null)
                throw new ArgumentException("File is required.");

            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            // Validate file size
            if (file.Length > MAX_FILE_SIZE_TICKET)
            {
                var maxSizeMB = MAX_FILE_SIZE_TICKET / (1024.0 * 1024.0);
                var fileSizeMB = file.Length / (1024.0 * 1024.0);
                throw new ArgumentException($"File size ({fileSizeMB:F2}MB) exceeds maximum allowed size ({maxSizeMB:F0}MB).");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("File must have an extension.");

            // Check for dangerous files
            if (DangerousExtensions.Contains(extension))
                throw new ArgumentException($"File type '{extension}' is not allowed for security reasons.");

            // Check if extension is allowed
            if (!AllowedTicketExtensions.Contains(extension))
            {
                var allowedTypes = string.Join(", ", AllowedTicketExtensions);
                throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {allowedTypes}");
            }

            // Validate MIME type (if provided)
            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                if (!AllowedTicketMimeTypes.Contains(file.ContentType))
                {
                    throw new ArgumentException($"File MIME type '{file.ContentType}' is not allowed.");
                }
            }
        }

        /// <summary>
        /// Validates file for document uploads
        /// </summary>
        public static void ValidateDocumentFile(IFormFile file)
        {
            if (file == null)
                throw new ArgumentException("File is required.");

            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            // Validate file size
            // file.Length is in bytes, MAX_FILE_SIZE_DOCUMENT is 100MB = 104,857,600 bytes
            // Debug: Log file size for troubleshooting
            var fileSizeBytes = file.Length;
            var fileSizeKB = fileSizeBytes / 1024.0;
            var fileSizeMB = fileSizeBytes / (1024.0 * 1024.0);
            
            if (fileSizeBytes > MAX_FILE_SIZE_DOCUMENT)
            {
                var maxSizeMB = MAX_FILE_SIZE_DOCUMENT / (1024.0 * 1024.0);
                throw new ArgumentException($"File quá lớn. Kích thước file: {fileSizeKB:F2}KB ({fileSizeMB:F2}MB), tối đa cho phép: {maxSizeMB:F0}MB.");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("File must have an extension.");

            // Check for dangerous files
            if (DangerousExtensions.Contains(extension))
                throw new ArgumentException($"File type '{extension}' is not allowed for security reasons.");

            // For documents, allow images + document types
            var allowedExtensions = new HashSet<string>(AllowedImageExtensions);
            allowedExtensions.UnionWith(AllowedDocumentExtensions);

            if (!allowedExtensions.Contains(extension))
            {
                var allowedTypes = string.Join(", ", allowedExtensions);
                throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {allowedTypes}");
            }
        }

        /// <summary>
        /// Validates file for general uploads (default validation)
        /// </summary>
        public static void ValidateGeneralFile(IFormFile file)
        {
            if (file == null)
                throw new ArgumentException("File is required.");

            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            // Validate file size
            if (file.Length > MAX_FILE_SIZE_GENERAL)
            {
                var maxSizeMB = MAX_FILE_SIZE_GENERAL / (1024.0 * 1024.0);
                var fileSizeMB = file.Length / (1024.0 * 1024.0);
                throw new ArgumentException($"File size ({fileSizeMB:F2}MB) exceeds maximum allowed size ({maxSizeMB:F0}MB).");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("File must have an extension.");

            // Check for dangerous files
            if (DangerousExtensions.Contains(extension))
                throw new ArgumentException($"File type '{extension}' is not allowed for security reasons.");
        }

        /// <summary>
        /// Gets allowed file extensions as a string for frontend accept attribute
        /// </summary>
        public static string GetTicketAcceptTypes()
        {
            return ".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf,.doc,.docx";
        }

        /// <summary>
        /// Gets allowed file extensions as a string for document uploads
        /// </summary>
        public static string GetDocumentAcceptTypes()
        {
            return ".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf,.doc,.docx,.xls,.xlsx,.txt,.csv";
        }
    }
}

