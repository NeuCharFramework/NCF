using DemoAudit.Models;

namespace DemoAudit.Repository
{
    public interface IAuditRepository
    {
        void InsertAuditLogs(AuditModel objauditmodel);
    }
}