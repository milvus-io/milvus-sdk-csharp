using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Milvus.Param.Credential
{
    public class CreateCredentialParam
    {
        public static CreateCredentialParam Create(string username,string password)
        {
            var param = new CreateCredentialParam()
            {
                Username = username,
                Password = password
            };
            param.Check();

            return param;
        }

        public string Username { get; set; }

        public string Password { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Username, "Username");
            ParamUtils.CheckNullEmptyString(Password, "Password");
        }
    }
}
