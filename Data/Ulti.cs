using Microsoft.AspNetCore.Components.Forms;
using System.Data.SqlTypes;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IIS.Core;
using OfficeOpenXml;

namespace Hop_dong_dien_tu.Data
{
    public class Ulti
    {
        public static readonly string connectionString = "Data Source = 192.168.9.2; Initial Catalog = EOSCT; User ID = hopdongdientu; Password=p08FrDFrD; MultipleActiveResultSets=true";

        public static (string? userName, string? TENNV) login(string? username, string? password)
        {
            try
            {
                username = username?.Trim();
                using SqlConnection sqlcon = new(connectionString);
                sqlcon.Open();
                byte[] encodedPassword = new UTF8Encoding().GetBytes(password);
                byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
                string encoded = BitConverter.ToString(hash).Replace("-", string.Empty).ToUpper();
                string s = @"SELECT TOP 1 B.HOTEN, A.Username
                        FROM UserAdmin A JOIN NHANVIEN B ON A.USERNAME = B.MANV
                        WHERE A.ACTIVE=1 AND UPPER(A.Username) = UPPER(@USERNAME) AND A.Password = @PASSWORD";
                SqlCommand sc = new SqlCommand(s, sqlcon);
                sc.Parameters.AddWithValue("USERNAME", username);
                sc.Parameters.AddWithValue("PASSWORD", encoded);
                SqlDataReader dr = sc.ExecuteReader();
                if (dr.HasRows)
                {
                    dr.Read();
                    return (dr["Username"]?.ToString(), dr["HOTEN"]?.ToString());


                }
                else
                    return ("", "");
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static (string? userNameMBF, string? passWordMBF) getMBFAccount(string MANV)
        {
            try
            {
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 [MANV], [USERNAME], [PASSWORD] 
                                                      FROM [NHANVIEN_MBF] 
                                                      WHERE MANV = @MANV", sqlcon);

                    cmd.Parameters.AddWithValue("@MANV", MANV);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return (dt.Rows[0]["USERNAME"].ToString(), dt.Rows[0]["PASSWORD"].ToString());
                    }
                    else
                    {
                        return ("", "");
                    }
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static void insertMBFAccount(string? MANV, string? userNameMBF, string? passWordMBF)
        {
            try
            {
                if (MANV == null || MANV == "" || userNameMBF == null || userNameMBF == "" || passWordMBF == null || passWordMBF == "")
                    throw new Exception("Chưa đủ thông tin tài khoản QLKH và tài khoản MBF");
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    SqlCommand cmd = new SqlCommand("IF NOT EXISTS (SELECT * FROM [EOSCT].[dbo].[NHANVIEN_MBF] WHERE MANV = @MANV) " +
                        "                               BEGIN INSERT INTO [EOSCT].[dbo].[NHANVIEN_MBF] (MANV, USERNAME, PASSWORD) " +
                        "                               VALUES (@MANV, @USERNAME, @PASSWORD) END " +
                        "                               ELSE BEGIN UPDATE [EOSCT].[dbo].[NHANVIEN_MBF] SET USERNAME = @USERNAME, PASSWORD = @PASSWORD WHERE MANV = @MANV END", sqlcon);

                    cmd.Parameters.AddWithValue("@MANV", MANV);
                    cmd.Parameters.AddWithValue("@USERNAME", userNameMBF);
                    cmd.Parameters.AddWithValue("@PASSWORD", passWordMBF);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public async static Task<TokenResponse?> getMBFToken(string? userNameMBF, string? passWordMBF)
        {
            try
            {
                if (userNameMBF == null || userNameMBF == "" || passWordMBF == null || passWordMBF == "")
                    throw new Exception("Vui lòng điền đầy đủ Username và Password");

                using (HttpClient client = new HttpClient())
                {

                    // Create a json object with the parameters
                    var json = new { email = userNameMBF, password = passWordMBF, type = 0 };

                    // Serialize the json object to a string
                    var jsonString = JsonConvert.SerializeObject(json);

                    // Create a StringContent object with the json string and content type
                    var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");

                    // Send the POST request to the API and get the response
                    var response = await client.PostAsync("https://mobifone-econtract.vn/service/api/v1/auth", content);

                    // Read the response content as a string
                    string responseString = await response.Content.ReadAsStringAsync();

                    // Deserialize the response string to a TokenResponse object
                    TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
                    client.Dispose();
                    return tokenResponse;
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public async static Task<List<eContractTemplate>?> geteContractTemplate(string access_Token, string email)
        {
            try
            {
                if (access_Token.Trim() == "" && email.Trim() == "")
                    throw new Exception("Tham số lấy token không hợp lệ");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_Token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var queryString = "?email=" + email;
                    var requestUri = "https://mobifone-econtract.vn/service/api/v1/tp/contracts/get-templates" + queryString;
                    HttpResponseMessage response = await client.GetAsync(requestUri);
                    client.Dispose();
                    if (response.IsSuccessStatusCode)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        List<eContractTemplate>? template = JsonConvert.DeserializeObject<List<eContractTemplate>>(responseString);
                        return template;
                    }
                    else
                    {
                        throw new Exception($"Status code: {response.StatusCode} - Reason phrase: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static string createExcelFromEcontract722(eContract722 input, string userName)
        {
            try
            {
                FileInfo fileInfo = new FileInfo("test.xlsx");
                using (ExcelPackage excel = new ExcelPackage())
                {
                    // Add a worksheet
                    excel.Workbook.Worksheets.Add("Sheet1");

                    // Get the worksheet
                    var worksheet = excel.Workbook.Worksheets["Sheet1"];

                    // Write the column headers
                    worksheet.Cells[1, 1].Value = "TENHOPDONG";
                    worksheet.Cells[1, 2].Value = "SOHOPDONG";
                    worksheet.Cells[1, 3].Value = "NGAYHETHANKY";
                    worksheet.Cells[1, 4].Value = "TENKH";
                    worksheet.Cells[1, 5].Value = "DIACHI";
                    worksheet.Cells[1, 6].Value = "IDKH";
                    worksheet.Cells[1, 7].Value = "DONGHO";
                    worksheet.Cells[1, 8].Value = "ONG";
                    worksheet.Cells[1, 9].Value = "NGAYLAPDAT";
                    worksheet.Cells[1, 10].Value = "NGAY";
                    worksheet.Cells[1, 11].Value = "THANG";
                    worksheet.Cells[1, 12].Value = "NAM";
                    worksheet.Cells[1, 13].Value = "CHINHANH";
                    worksheet.Cells[1, 14].Value = "SODTCHINHANH";
                    worksheet.Cells[1, 15].Value = "DAIDIENBENB";
                    worksheet.Cells[1, 16].Value = "DIACHIBENB";
                    worksheet.Cells[1, 17].Value = "DAIDIENBENA";
                    worksheet.Cells[1, 18].Value = "EMAILBENA";
                    worksheet.Cells[1, 19].Value = "DAIDIENBENB";
                    worksheet.Cells[1, 20].Value = "SODTNGUOIKY";

                    // Write the input values
                    worksheet.Cells[2, 1].Value = input.TENHOPDONG;
                    worksheet.Cells[2, 2].Value = input.SOHOPDONG;
                    worksheet.Cells[2, 3].Value = input.NGAYHETHANKY;
                    worksheet.Cells[2, 4].Value = input.TENKH;
                    worksheet.Cells[2, 5].Value = input.DIACHI;
                    worksheet.Cells[2, 6].Value = input.IDKH;
                    worksheet.Cells[2, 7].Value = input.DONGHO;
                    worksheet.Cells[2, 8].Value = input.ONG;
                    worksheet.Cells[2, 9].Value = input.NGAYLAPDAT;
                    worksheet.Cells[2, 10].Value = input.NGAY;
                    worksheet.Cells[2, 11].Value = input.THANG;
                    worksheet.Cells[2, 12].Value = input.NAM;
                    worksheet.Cells[2, 13].Value = input.CHINHANH;
                    worksheet.Cells[2, 14].Value = input.SODTCHINHANH;
                    worksheet.Cells[2, 15].Value = input.DAIDIENBENB;
                    worksheet.Cells[2, 16].Value = input.DIACHIBENB;
                    worksheet.Cells[2, 17].Value = input.DAIDIENBENA;
                    worksheet.Cells[2, 18].Value = input.EMAILBENA;
                    worksheet.Cells[2, 19].Value = input.DAIDIENBENB;
                    worksheet.Cells[2, 20].Value = input.SODTNGUOIKY;

                    // Save the file
                    string fileName = userName + "_Hopdong.xlsx";
                    FileInfo excelFile = new FileInfo(fileName);
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                    excel.SaveAs(excelFile);

                    return fileName;
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public async static Task<eContractResponse?> createMBFEcontract(string access_Token, string filePath, string templateId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set the authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_Token);

                    // Create a new MultipartFormDataContent
                    using (var content = new MultipartFormDataContent())
                    {
                        // Add the excel file as a stream content
                        if (!File.Exists(filePath))
                            throw new Exception("File ko ton tai");
                        using (var fileStream = File.OpenRead(filePath))
                        {
                            var fileContent = new StreamContent(fileStream);
                            content.Add(fileContent, "file", filePath);
                            var response = await client.PostAsync("https://mobifone-econtract.vn/service/api/v1/tp/contracts/batch?templateId=" + templateId, content);
                            if (response.IsSuccessStatusCode)
                            {
                                string responseString = await response.Content.ReadAsStringAsync();
                                eContractResponse? result = new eContractResponse();
                                try
                                {
                                    result = JsonConvert.DeserializeObject<eContractResponse>(responseString);
                                }
                                catch (Exception b)
                                { }

                                if (result?.success != null && result.success == false)
                                {
                                    if (result?.details != null && result?.details.Count > 0)
                                    {
                                        string details = "";
                                        foreach (var item in result.details)
                                        {
                                            details += item.ToString() + "\r";
                                        }
                                        throw new Exception(details);
                                    }
                                    else
                                        throw new Exception("Tạo hợp đồng thất bại!");
                                }
                                List<eContractResponse>? result2 = new List<eContractResponse>();
                                try
                                {
                                    result2 = JsonConvert.DeserializeObject<List<eContractResponse>>(responseString);
                                }
                                catch (Exception c)
                                { }
                                if (result2 != null && result2.Count > 0)
                                {
                                    return result2[0];
                                }
                                return result;
                            }
                            else
                            {
                                //throw new Exception($"Status code: {response.StatusCode} - Reason phrase: {response.ReasonPhrase}");
                                throw new Exception("Tạo hợp đồng thất bại!");
                            }
                        }
                    }
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static async Task<List<eContractFile>?> getEcontractPDFFile(string access_Token, string eContractID)
        {
            try {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_Token);
                    var url = "https://mobifone-econtract.vn/service/api/v1/tp/contracts/" + eContractID + "/documents";
                    var method = HttpMethod.Get;
                    var request = new HttpRequestMessage(method, url);
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        List<eContractFile>? files = JsonConvert.DeserializeObject<List<eContractFile>>(responseString);
                        return files;
                    }
                    else
                    {
                        throw new Exception($"Status code: {response.StatusCode} - Reason phrase: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static async Task<UploadSignImage_Response?> uploadSignImage(string ACCESSTOKEN, string ORGID, string IMGNAME, string IMGCONTENT)
        {
            try {
                string url = "https://mobifone-econtract.vn/service/api/v1/tp/files/organization/" + ORGID;                
                var json = new { name = IMGNAME, content = IMGCONTENT};
                var jsonBody = JsonConvert.SerializeObject(json);
                UploadSignImage_Response? result = new UploadSignImage_Response();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ACCESSTOKEN);
                    //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpContent httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(url, httpContent);
                    string responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<UploadSignImage_Response>(responseContent);
                    return result;
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

         

        public static Customer? getCustomerInfo(string IDKH)
        {
            try
            {
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    SqlCommand cmd = new SqlCommand("SELECT TOP 1 A.IDKH, A.MADP, A.MADB, A.TENKH, " +
                        "case when A.INHD_SDTTKHAC=1 then a.INHD_DIACHI else a.SONHA + ' - ' + B.TENDP end as DIACHI, " +
                        "A.SODT, A.MALKHDB, A.SONK, A.NGAYTAO, A.NGAYDK, A.MAKV, C.TENDH, D.TENKICHCO " +
                        "FROM KHACHHANG A JOIN DUONGPHO B ON A.MADP=B.MADP " +
                        "JOIN DONGHO C ON A.MADH=C.MADH " +
                        "JOIN DONGHOKICHCO D ON A.KICHCODH = D.KICHCODH " +
                        "WHERE IDKH=@IDKH", sqlcon);
                    cmd.Parameters.AddWithValue("@IDKH", int.Parse(IDKH));
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    Customer customer = new Customer();
                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        customer.IDKH = (int)row["IDKH"];
                        customer.MADP = (string)row["MADP"];
                        customer.MADB = (string)row["MADB"];
                        customer.TENKH = (string)row["TENKH"];
                        customer.DIACHI = (string)row["DIACHI"];
                        customer.SODT = (string)row["SODT"];
                        customer.MALKHDB = (string)row["MALKHDB"];
                        customer.SONK = (short)row["SONK"];
                        customer.NGAYTAO = row.IsNull("NGAYTAO") ? null : (DateTime?)row["NGAYTAO"];
                        customer.NGAYDK = row.IsNull("NGAYDK") ? null : (DateTime?)row["NGAYDK"];
                        customer.MAKV = (string)row["MAKV"];
                        customer.TENDH = row.IsNull("TENDH") ? null : (string)row["TENDH"];
                        customer.TENKICHCO = row.IsNull("TENKICHCO") ? null : (string)row["TENKICHCO"];
                        return customer;
                    }
                    else return null;
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static List<string> getDauSoDiDong()
        {
            try
            {
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    List<string> result = new List<string>();
                    string s = @"SELECT TOP (1000) [AGENTID]
                                      ,[AGENTNUMBER]
                                  FROM [SMS_AGENT_NUMBER]";
                    SqlDataAdapter da = new SqlDataAdapter(s, sqlcon);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            result.Add(dt.Rows[i]["AGENTNUMBER"].ToString());
                        }
                    return result;
                }
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public static List<string>? findPhoneNumberFromText(string inputText, List<string> DSDauSo)
        {
            try
            {
                if (DSDauSo.Count > 0)
                {
                    string sample = "";
                    for (int i = 0; i < DSDauSo.Count; i++)
                    {
                        string dauso = DSDauSo[i];
                        sample += ((sample.Length > 0 ? "|" : "") + dauso + "\\d{7}");
                    }
                    //var exp = new Regex(@"0(1\d{9}|9\d{8})", RegexOptions.IgnoreCase);
                    //var exp = new Regex(@"(0|84)(1\d{9}|9\d{8})", RegexOptions.IgnoreCase);
                    var exp = new Regex(@"(0|84)(" + sample + ")", RegexOptions.IgnoreCase);
                    var text = inputText.Replace(".", "").Replace(" ", "");

                    var matchList = exp.Matches(text).Cast<Match>()
                      .Select(m => m.Groups[0].Value)
                      .ToList();
                    if (matchList.Count > 0)
                    {
                        return matchList;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                    return null;
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }
    }

    public class Identity
    {
        public string? USERNAME = "";
        public string? TENNV = "";
        public string? USERNAMEMBF = "";
        public string? PASSWORDMBF = "";
        public string? ACCESSTOKEN = "";
        public string pageTitle = "";
        public string ORGID = "2346"; //ID của tổ chức Cấp nước Cần Thơ 2 trên trang eContract của Mobifone
        private int _currentPage = 0;
        public int currentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                updateLayout();
            }
        }

        public Task<bool> updateLayout()
        {
            try
            {
                updateMainLayout();
                updateIndexLayout();
                return Task.FromResult(true);
            }
            catch (Exception a)
            {
                throw new Exception(a.Message);
            }
        }

        public Func<Task<bool>> updateMainLayout = () => Task.FromResult(true);
        public Func<Task<bool>> updateIndexLayout = () => Task.FromResult(true);
        /*
         0 - Login QLKH
         1 - Login mobifone
         2 - Hợp đồng điện tử
        */

        public void clear()
        {
            USERNAME = TENNV = USERNAMEMBF = PASSWORDMBF = ACCESSTOKEN = pageTitle = "";
            currentPage = 0;
        }
    }

    public class LoadingScreen
    {
        public bool isShown = false;
        public Func<Task<bool>> updateState = () => { return Task.FromResult(true); };
        public void setUpdateFunction(Func<Task<bool>> a)
        {
            updateState = a;
        }

        public async Task<bool> Show()
        {
            isShown = true;
            return await updateState();
        }

        public async Task<bool> Hide()
        {
            isShown = false;
            return await updateState();
        }
    }

    public class TokenResponse
    {
        public string? code = "";

        public string? type = "";

        public string? access_token = "";

        public CustomerMBF customer = new CustomerMBF();

        public int? login_fail_num = 0;

        public DateTime? active_at = null;
    }

    public class CustomerMBF
    {

        public int? type = 0;
        public Info info = new Info();
    }

    public class Info
    {
        public int? organizationId = 0;
        public bool? passwordChange = false;
        public string? phone = "";
        public string? accessKey = "";
        public string? name = "";
        public int? typeId = 0;
        public int? shareId = 0;
        public int? id = 0;
        public int? organizationChange = 0;
        public string? email = "";
    }

    public class Customer
    {
        public int IDKH = 0;
        public string MADP = "";
        public string MADB = "";
        public string TENKH = "";
        public string DIACHI = "";
        public string SODT = "";
        public string MALKHDB = "";
        public int SONK = 0;
        public string? TENDH = "";
        public string? TENKICHCO = "";
        public DateTime? NGAYTAO = null;
        public DateTime? NGAYDK = null;
        public string MAKV = "";
    }

    public class CustomerMBFAccount
    {
        public string USERNAME = "", PASSWORD = "", ACCESSTOKEN = "", CONTRACTID = "", SIGNIMAGE = "" /*hình chữ ký tay dạng base64*/;
    }

    public class SignData {
        public string otp = "";
        public string signInfo = "";
        public string processAt = "";//Ngày giờ ký dạng "2023-07-06T14:08:25Z"
        public SignAreaInfo fields = new SignAreaInfo();
    }

    public class SignAreaInfo
    {
        public int id = 0, font_size = 0;
        public string name = "",
                        value = ""/*đường dẫn chữ ký ảnh nhận được sau khi upload lên API*/,
                        font = "",
                        bucket = ""/*thuộc tính của file_object sau khi upload ảnh lên API*/;
    }

    public class eContractTemplate
    {
        public string? id = "";
        public string? name = "";
    }

    public class UploadSignImage_Response
    {
        string? success = null, message = null;
        UploadSignImage_Response_FileObject? file_object = new UploadSignImage_Response_FileObject();
    }

    public class UploadSignImage_Response_FileObject
    {
        string? filename=null, bucket = null, file_path = null;
    }

    public class GetOTP_Response {
        bool? success = null, locked = null;
        string? message = null, nextAttempt = null /*Nếu locked là true thì nextAttempt là thời gian user hết bị khóa*/;
    }

    public class eContract722
    {
        public string TENHOPDONG = "",
            SOHOPDONG = "",
            NGAYHETHANKY = "",
            TENKH = "",
            DIACHI = "",
            IDKH = "",
            DONGHO = "",
            ONG = "",
            NGAYLAPDAT = "",
            NGAY = "",
            THANG = "",
            NAM = "",
            CHINHANH = "",
            SODTCHINHANH = "",
            DAIDIENBENB = "",
            DIACHIBENB = "",
            DAIDIENBENA = "",
            EMAILBENA = "",
            SODTNGUOIKY = "";
    }

    public class eContractResponse
    {
        public string? id = "";
        public string? name = "";
        public List<eContractParticipants>? participants = new List<eContractParticipants>();
        public string? organization_id = "";
        public bool? success = true; //Trường lúc tạo hợp đồng thất bại
        public string? message = ""; //Trường lúc tạo hợp đồng thất bại
        public List<string>? details = new List<string>(); //Trường lúc tạo hợp đồng thất bại        
    }

    public class eContractParticipants
    {
        public string? id="", name = "", type = "", ordering = "";
        public List<eContractRecipient>? recipients = new List<eContractRecipient>();
    }

    public class eContractRecipient 
    {
        public string? id="", name = "", phone = "", username = "", password = "";
    }

    public class  eContractFile
    {
        public string? id = "";
        public string? type = "";
        public string? path = "";
        public string? contract_id = "";
        public string? internal_path = "";
    }
}
