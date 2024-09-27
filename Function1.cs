using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient; // DB接続用ライブラリ
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
            // インサート用のパラメーター取得（GETメソッド用）
            string id = req.Query["ID"];
            string password = req.Query["Password"];

            // インサート用のパラメーター取得（POSTメソッド用）
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            id = id ?? data?.ID;
            password = password ?? data?.Password;

            // パラメーターが設定されていない場合はエラーメッセージを返す
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                return new BadRequestObjectResult(new
                {
                    Message = "ID または Password が指定されていません。",
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                // DB接続設定（接続文字列の構築）
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = "m3h-tomokimure-server.database.windows.net",
                    UserID = "torutoru2004",
                    Password = "Tomoki0526",
                    InitialCatalog = "m3h-Tomoki-ultimateDB"
                };

                // 接続用オブジェクトの初期化
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    // 実行するクエリ（パラメーター化）
                    string sql = "SELECT COUNT(1) FROM MemberTable WHERE ID = @ID AND Password = @Password";

                    // SQL実行オブジェクトの初期化
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // パラメーターの追加
                        command.Parameters.AddWithValue("@ID", id);
                        command.Parameters.AddWithValue("@Password", password);

                        // DBと接続
                        await connection.OpenAsync(); // 非同期メソッドで接続を開く

                        // SQLを実行し、結果を確認
                        int count = (int)await command.ExecuteScalarAsync(); // 非同期メソッドでSQLを実行

                        if (count > 0)
                        {
                            // IDとPasswordが一致する場合
                            return new OkObjectResult(new
                            {
                                Message = "OK",
                                StatusCode = StatusCodes.Status200OK
                            });
                        }
                        else
                        {
                            // IDまたはPasswordが一致しない場合
                            return new UnauthorizedObjectResult(new
                            {
                                Message = "ID または Password が一致しません。",
                                StatusCode = StatusCodes.Status401Unauthorized
                            });
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                // エラーをログに出力
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

            // HTTPレスポンスで返す文字列を定義
            string responseMessage = "INSERT RESULT:";

            // POSTメソッド用のインサート用のパラメーター取得
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string nickname = data?.Nickname;
            string id = data?.ID;
            string password = data?.Password;

            // パラメーターが全て取得できた場合のみ処理
            if (!string.IsNullOrWhiteSpace(nickname) && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    // DB接続設定（接続文字列の構築）
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                    {
                        DataSource = "m3h-tomokimure-server.database.windows.net",
                        UserID = "torutoru2004",
                        Password = "Tomoki0526",
                        InitialCatalog = "m3h-Tomoki-ultimateDB"
                    };

                    // SQLコネクションを初期化
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        // 実行するSQL（パラメーター付き）
                        string sql = "INSERT INTO MemberTable (Nickname, ID, Password) VALUES (@Nickname, @ID, @Password)";

                        // SQLコマンドを初期化
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            // パラメーターを設定
                            command.Parameters.AddWithValue("@Nickname", nickname);
                            command.Parameters.AddWithValue("@ID", id);
                            command.Parameters.AddWithValue("@Password", password);

                            // コネクションオープン（＝　SQLDatabaseに接続）
                            await connection.OpenAsync(); // 非同期メソッドで接続を開く

                            // SQLコマンドを実行し結果行数を取得
                            int result = await command.ExecuteNonQueryAsync(); // 非同期メソッドでSQLを実行

                            // 結果をレスポンスに格納
                            responseMessage = responseMessage + $"{result}行挿入されました";
                        }
                    }
                }
                // DB処理でエラーが発生した場合
                catch (SqlException e)
                {
                    // エラーをレスポンスに格納
                    responseMessage = "データベースエラーが発生しました: " + e.Message;
                    log.LogError(e.ToString());
                }
            }
            else
            {
                responseMessage = "必要なパラメーターが設定されていません";
            }

            // HTTPレスポンスを返却
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
                // DB接続設定（接続文字列の構築）
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = "m3h-tomokimure-server.database.windows.net",
                    UserID = "torutoru2004",
                    Password = "Tomoki0526",
                    InitialCatalog = "m3h-Tomoki-ultimateDB"
                };

                // 接続用オブジェクトの初期化
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    // 実行するクエリ（ランダムに1件取得）
                    string sql = "SELECT TOP 1 Result FROM FortuneResults ORDER BY NEWID()";

                    // SQL実行オブジェクトの初期化
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // DBと接続
                        await connection.OpenAsync(); // 非同期メソッドで接続を開く

                        // SQLを実行し、結果を取得
                        string result = (string)await command.ExecuteScalarAsync(); // 非同期メソッドでSQLを実行

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
                // エラーをログに出力
                log.LogError(e.ToString());

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
