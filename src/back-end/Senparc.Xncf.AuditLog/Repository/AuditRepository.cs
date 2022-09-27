using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using DemoAudit.Models;
using Microsoft.Extensions.Configuration;

namespace DemoAudit.Repository
{
    public class AuditRepository : IAuditRepository
    {
        private readonly IConfiguration _configuration;
        public AuditRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void InsertAuditLogs(AuditModel objauditmodel)
        {
            using SqlConnection con = new SqlConnection("Data Source= (local); initial catalog=NCF; user id=sa; password=123456");
            try
            {
                SqlCommand cmd = con.CreateCommand();
                con.Open();

                string sql = "select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Audit'";
                SqlCommand command = new SqlCommand(sql, con);//调用公共类中的ExceRead方法创建数据阅读器
                var Read = command.ExecuteScalar();
                if (Read == null)//加个判断,没有则创建表
                {
                    //sql = "create table [Audit]([NO.] [int] identity(1,1))";//([NO.] [int] identity(1,1))";//创建表 //identity(1,1)自增ID
                    sql = "create table [Audit]([AuditId][bigint] IDENTITY(1, 1) NOT NULL, [Area] [varchar] (50) NULL, [ControllerName][varchar] (50) NULL, [ActionName][varchar] (50) NULL, [LoginStatus][varchar] (1) NULL, [LoggedInAt][varchar] (23) NULL, [LoggedOutAt][varchar] (23) NULL, [PageAccessed][varchar] (500) NULL, [IPAddress][varchar] (50) NULL, [SessionID][varchar] (50) NULL, [UserID][varchar] (50) NULL, [RoleId][varchar] (2) NULL, [LangId][varchar] (2) NULL, [IsFirstLogin][varchar] (2) NULL, [CurrentDatetime][varchar] (23) NULL)";//([NO.] [int] identity(1,1))";//创建表 //identity(1,1)自增ID
                    using (SqlConnection Conn = new SqlConnection(_configuration.GetConnectionString("AuditDatabaseConnection")))
                    {
                        Conn.Open();
                        command = new SqlCommand(sql, Conn);
                        command.ExecuteNonQuery();
                    }

                }

                cmd.CommandText = @"INSERT INTO Audit(Area,ControllerName,ActionName,LoginStatus,LoggedInAt,LoggedOutAt,PageAccessed,IPAddress,SessionID,UserID,RoleId,LangId,IsFirstLogin,CurrentDatetime) Values(@Area,@ControllerName,@ActionName,@LoginStatus,@LoggedInAt,@LoggedOutAt,@PageAccessed,@IPAddress,@SessionID,@UserID,@RoleId,@LangId,@IsFirstLogin,@CurrentDatetime)";//插入积分

                cmd.Parameters.Add(new SqlParameter("@UserID", objauditmodel.UserId));

                cmd.Parameters.Add(new SqlParameter("@SessionID", objauditmodel.SessionId));

                cmd.Parameters.Add(new SqlParameter("@IPAddress", objauditmodel.IpAddress));

                cmd.Parameters.Add(new SqlParameter("@PageAccessed", objauditmodel.PageAccessed));

                cmd.Parameters.Add(new SqlParameter("@LoggedInAt", DBNull.Value));

                cmd.Parameters.Add(new SqlParameter("@LoggedOutAt", DBNull.Value));

                cmd.Parameters.Add(new SqlParameter("@LoginStatus", objauditmodel.LoginStatus));

                cmd.Parameters.Add(new SqlParameter("@ControllerName", objauditmodel.ControllerName));

                cmd.Parameters.Add(new SqlParameter("@ActionName", objauditmodel.ActionName));

                cmd.Parameters.Add(new SqlParameter("@UrlReferrer", DBNull.Value));

                cmd.Parameters.Add(new SqlParameter("@Area", DBNull.Value));

                cmd.Parameters.Add(new SqlParameter("@RoleId", objauditmodel.RoleId));

                cmd.Parameters.Add(new SqlParameter("@LangId", objauditmodel.LangId));

                cmd.Parameters.Add(new SqlParameter("@IsFirstLogin", objauditmodel.IsFirstLogin));

                cmd.Parameters.Add(new SqlParameter("@CurrentDateTime", DBNull.Value));

                cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}