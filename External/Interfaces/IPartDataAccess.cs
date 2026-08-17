using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    public interface IPartDataAccess
    {
        public List<Part> GetParts();
        public Part GetPartById(int partId);
        public bool DeletePartById(int partId);
        public bool SavePart(Part part);
    }
}
