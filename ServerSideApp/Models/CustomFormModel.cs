namespace ServerSideApp.Models
{
    /// <summary>
    /// MyForm model
    /// </summary>
    public class CustomFormModel
    {
        public string TrustedFilePath { get; set; }
        public string TrustedFileName { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
        public string Guid { get; private set; } = System.Guid.NewGuid().ToString();
        public bool IsPrimary { get; set; }

        public override string ToString()
        {
            return $"{nameof(TrustedFilePath)}: [{TrustedFilePath}];" + Environment.NewLine +
                   $"{nameof(UserId)}: {UserId}; " + Environment.NewLine +
                   $"{nameof(Guid)}: {Guid}; " + Environment.NewLine +
                   $"{nameof(IsPrimary)}: {IsPrimary}; ";
        }
    }
}
