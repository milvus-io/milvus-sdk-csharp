namespace IO.Milvus.Param
{
    /// <remarks>
    /// https://stackoverflow.com/questions/2912340/c-sharp-hashcode-builder
    /// </remarks>
    public sealed class HashCodeBuilder
    {
        private int hash = 17;

        public HashCodeBuilder Add(int value)
        {
            unchecked
            {
                hash = hash * 31 + value; //see Effective Java for reasoning
                                          // can be any prime but hash * 31 can be opimised by VM to hash << 5 - hash
            }
            return this;
        }

        public HashCodeBuilder Add(object value)
        {
            return Add(value != null ? value.GetHashCode() : 0);
        }

        public HashCodeBuilder Add(float value)
        {
            return Add(value.GetHashCode());
        }

        public HashCodeBuilder Add(double value)
        {
            return Add(value.GetHashCode());
        }

        public override int GetHashCode()
        {
            return hash;
        }
    }
}
