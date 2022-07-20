namespace IO.Milvus.Param.Credential
{
    public class DeleteCredentialParam
    {
        public static DeleteCredentialParam Create(string username)
        {
            var param = new DeleteCredentialParam()
            {
                Username = username
            };
            param.Check();

            return param;
        }

        public string Username { get; set; }

        internal void Check()
        {
            ParamUtils.CheckNullEmptyString(Username, "Username");
        }
    }
}
