using System.Threading.Tasks;
using Maruko.Core.FreeSql.Internal.AppService;

namespace Cbb.Application
{
    public interface IAllCountryOilPriceService : ICurdAppService<AppAllCountryOilPrice, AppAllCountryOilPriceDTO>
    {
        Task SpiderOil();
    }
}