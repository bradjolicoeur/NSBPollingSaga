using TargetAPI.Models;

namespace TargetAPI.Repository
{
    public interface IRequestRepository
    {
        RequestState CheckRequest(string claimCheck);
        RequestState Insert(ValueRequest model);
    }
}