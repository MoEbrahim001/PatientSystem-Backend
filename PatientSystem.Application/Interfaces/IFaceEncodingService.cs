using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientSystem.Application.Interfaces
{
    public interface IFaceEncodingService
    {
        Task<string> GenerateEncodingAsync(
             int patientId,
             string faceImageFullPath
         );

        Task ReloadEncodingsAsync();
    }
}
