using MidnightApi.DataAccess;

namespace MidnightApi.Models;

public class CommonModel
{
    public class BaseFamilyInput
    {
        [DbParam("p_FK_Families")]
        public long FamilyId { get; set; }
    }
}
