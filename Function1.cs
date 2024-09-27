using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient; // DB�ڑ��p���C�u����
using Newtonsoft.Json;

namespace FunctionAPIApp
{
    public static class Function1
    {
        [FunctionName("SELECT")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // �C���T�[�g�p�̃p�����[�^�[�擾�iGET���\�b�h�p�j
            string id = req.Query["ID"];
            string password = req.Query["Password"];

            // �C���T�[�g�p�̃p�����[�^�[�擾�iPOST���\�b�h�p�j
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            id = id ?? data?.ID;
            password = password ?? data?.Password;

            // �p�����[�^�[���ݒ肳��Ă��Ȃ��ꍇ�̓G���[���b�Z�[�W��Ԃ�
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                return new BadRequestObjectResult(new
                {
                    Message = "ID �܂��� Password ���w�肳��Ă��܂���B",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                // DB�ڑ��ݒ�i�ڑ�������̍\�z�j
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = "m3h-tomokimure-server.database.windows.net",
                    UserID = "torutoru2004",
                    Password = "Tomoki0526",
                    InitialCatalog = "m3h-Tomoki-ultimateDB"
                };

                // �ڑ��p�I�u�W�F�N�g�̏�����
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    // ���s����N�G���i�p�����[�^�[���j
                    string sql = "SELECT COUNT(1) FROM MemberTable WHERE ID = @ID AND Password = @Password";

                    // SQL���s�I�u�W�F�N�g�̏�����
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // �p�����[�^�[�̒ǉ�
                        command.Parameters.AddWithValue("@ID", id);
                        command.Parameters.AddWithValue("@Password", password);

                        // DB�Ɛڑ�
                        await connection.OpenAsync(); // �񓯊����\�b�h�Őڑ����J��

                        // SQL�����s���A���ʂ��m�F
                        int count = (int)await command.ExecuteScalarAsync(); // �񓯊����\�b�h��SQL�����s

                        if (count > 0)
                        {
                            // ID��Password����v����ꍇ
                            return new OkObjectResult(new
                            {
                                Message = "OK",
                                StatusCode = StatusCodes.Status200OK
                            });
                        }
                        else
                        {
                            // ID�܂���Password����v���Ȃ��ꍇ
                            return new UnauthorizedObjectResult(new
                            {
                                Message = "ID �܂��� Password ����v���܂���B",
                                StatusCode = StatusCodes.Status401Unauthorized
                            });
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                // �G���[�����O�ɏo��
                log.LogError(e.ToString());

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("INSERT")]
        public static async Task<IActionResult> RunInsert(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // HTTP���X�|���X�ŕԂ���������`
            string responseMessage = "INSERT RESULT:";

            // POST���\�b�h�p�̃C���T�[�g�p�̃p�����[�^�[�擾
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string nickname = data?.Nickname;
            string id = data?.ID;
            string password = data?.Password;

            // �p�����[�^�[���S�Ď擾�ł����ꍇ�̂ݏ���
            if (!string.IsNullOrWhiteSpace(nickname) && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    // DB�ڑ��ݒ�i�ڑ�������̍\�z�j
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                    {
                        DataSource = "m3h-tomokimure-server.database.windows.net",
                        UserID = "torutoru2004",
                        Password = "Tomoki0526",
                        InitialCatalog = "m3h-Tomoki-ultimateDB"
                    };

                    // SQL�R�l�N�V������������
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        // ���s����SQL�i�p�����[�^�[�t���j
                        string sql = "INSERT INTO MemberTable (Nickname, ID, Password) VALUES (@Nickname, @ID, @Password)";

                        // SQL�R�}���h��������
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            // �p�����[�^�[��ݒ�
                            command.Parameters.AddWithValue("@Nickname", nickname);
                            command.Parameters.AddWithValue("@ID", id);
                            command.Parameters.AddWithValue("@Password", password);

                            // �R�l�N�V�����I�[�v���i���@SQLDatabase�ɐڑ��j
                            await connection.OpenAsync(); // �񓯊����\�b�h�Őڑ����J��

                            // SQL�R�}���h�����s�����ʍs�����擾
                            int result = await command.ExecuteNonQueryAsync(); // �񓯊����\�b�h��SQL�����s

                            // ���ʂ����X�|���X�Ɋi�[
                            responseMessage = responseMessage + $"{result}�s�}������܂���";
                        }
                    }
                }
                // DB�����ŃG���[�����������ꍇ
                catch (SqlException e)
                {
                    // �G���[�����X�|���X�Ɋi�[
                    responseMessage = "�f�[�^�x�[�X�G���[���������܂���: " + e.Message;
                    log.LogError(e.ToString());
                }
            }
            else
            {
                responseMessage = "�K�v�ȃp�����[�^�[���ݒ肳��Ă��܂���";
            }

            // HTTP���X�|���X��ԋp
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GET_FORTUNE_RESULT")]
        public static async Task<IActionResult> RunGetFortuneResult(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for fortune result.");

            try
            {
                // DB�ڑ��ݒ�i�ڑ�������̍\�z�j
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = "m3h-tomokimure-server.database.windows.net",
                    UserID = "torutoru2004",
                    Password = "Tomoki0526",
                    InitialCatalog = "m3h-Tomoki-ultimateDB"
                };

                // �ڑ��p�I�u�W�F�N�g�̏�����
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    // ���s����N�G���i�����_����1���擾�j
                    string sql = "SELECT TOP 1 Result FROM FortuneResults ORDER BY NEWID()";

                    // SQL���s�I�u�W�F�N�g�̏�����
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // DB�Ɛڑ�
                        await connection.OpenAsync(); // �񓯊����\�b�h�Őڑ����J��

                        // SQL�����s���A���ʂ��擾
                        string result = (string)await command.ExecuteScalarAsync(); // �񓯊����\�b�h��SQL�����s

                        return new OkObjectResult(new
                        {
                            FortuneResult = result,
                            StatusCode = StatusCodes.Status200OK
                        });
                    }
                }
            }
            catch (SqlException e)
            {
                // �G���[�����O�ɏo��
                log.LogError(e.ToString());

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
