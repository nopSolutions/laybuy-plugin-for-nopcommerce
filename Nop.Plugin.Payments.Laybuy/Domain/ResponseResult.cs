using System.Runtime.Serialization;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents response result enumeration
    /// </summary>
    public enum ResponseResult
    {
        /// <summary>
        /// Request failed
        /// </summary>
        [EnumMember(Value = "error")]
        Error,

        /// <summary>
        /// Request was successful
        /// </summary>
        [EnumMember(Value = "success")]
        Success,

        /// <summary>
        /// Request was declined
        /// </summary>
        [EnumMember(Value = "declined")]
        Declined,

        /// <summary>
        /// Request was cancelled
        /// </summary>
        [EnumMember(Value = "cancelled")]
        Cancelled
    }
}