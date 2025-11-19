using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(KTXSV.Startup))]
namespace KTXSV
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Cấu hình Hangfire với SQL Server
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("HangfireConnection");

            // Dashboard để quản lý job
            app.UseHangfireDashboard("/hangfire");

            // Start Hangfire server
            app.UseHangfireServer();

            // Job định kỳ: chạy CheckExpiringContracts mỗi ngày lúc 00:00
            RecurringJob.AddOrUpdate(
                "CheckExpiringContractsJob",
                () => RunCheckExpiringContracts(),
                Cron.Daily
            );
        }

        // Wrapper để gọi method từ controller
        public void RunCheckExpiringContracts()
        {
            using (var db = new Models.KTXSVEntities())
            {
                var controller = new Controllers.AdminRoomsController();
                controller.CheckExpiringContracts();
            }
        }
    }
}
