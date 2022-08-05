namespace IO.Milvus.Param.Credential
{
    public class UpdateCredentialParam
    {
        public static UpdateCredentialParam Create(
            string username, 
            string oldPassword,
            string newPassword)
        {
            var param = new UpdateCredentialParam()
            {
                Username = username,
                OldPassword = oldPassword,
                NewPassword = newPassword
            };
            param.Check();

            return param;
        }

        public string Username { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Username, "Username");
            ParamUtils.CheckNullEmptyString(OldPassword, nameof(OldPassword));
            ParamUtils.CheckNullEmptyString(NewPassword, nameof(NewPassword));
        }
    }
}
