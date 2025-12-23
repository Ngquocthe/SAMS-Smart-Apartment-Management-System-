using Hangfire;
using Microsoft.Data.SqlClient;

namespace SAMS_BE.Helpers
{
    public static class HangfireHelper
    {
        /// <summary>
        /// Clears all Hangfire recurring job data from the database to prevent PRIMARY KEY constraint violations
        /// </summary>
        public static async Task ClearAllRecurringJobsAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                
                try
                {
                    // Delete from Hash table (this is where the PRIMARY KEY constraint error occurs)
                    var deleteHashCommand = new SqlCommand(
                        @"IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Hash' AND schema_id = SCHEMA_ID('HangFire'))
                          DELETE FROM [HangFire].[Hash] WHERE [Key] LIKE 'recurring-job:%'",
                        connection, transaction);
                    await deleteHashCommand.ExecuteNonQueryAsync();

                    // Delete from Set table
                    var deleteSetCommand = new SqlCommand(
                        @"IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Set' AND schema_id = SCHEMA_ID('HangFire'))
                          DELETE FROM [HangFire].[Set] WHERE [Key] = 'recurring-jobs'",
                        connection, transaction);
                    await deleteSetCommand.ExecuteNonQueryAsync();

                    // Commit transaction
                    transaction.Commit();
                    
                    Console.WriteLine("✓ Hangfire recurring jobs data cleared successfully");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"⚠ Warning: Transaction rolled back: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Could not clear Hangfire data: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely removes a recurring job if it exists
        /// </summary>
        public static void SafeRemoveRecurringJob(string jobId)
        {
            try
            {
                RecurringJob.RemoveIfExists(jobId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Could not remove job '{jobId}': {ex.Message}");
            }
        }
    }
}

