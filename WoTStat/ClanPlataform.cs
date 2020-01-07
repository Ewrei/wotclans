using System.Runtime.Serialization;

namespace Negri.Wot
{
    public class ClanPlataform
    {
        public ClanPlataform(Platform platform, long clanId, string clanTag)
        {
            Plataform = platform;
            ClanTag = clanTag;
            ClanId = clanId;
        }

        public Platform Plataform { get; protected set; }

        public string ClanTag { get; protected set; }

        /// <summary>
        /// Id num�rico do cl�
        /// </summary>
        public long ClanId { get; protected set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{ClanTag}@{Plataform}({ClanId})";
        }
    }

}