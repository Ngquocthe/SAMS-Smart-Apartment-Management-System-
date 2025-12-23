using Microsoft.AspNetCore.Http;
using SAMS_BE.DTOs;

namespace SAMS_BE.Interfaces.IService;

public interface IFaceRecognitionService
{
    Task<FaceVerifyResponseDto> VerifyFaceAsync(FaceVerifyRequestDto request);
    Task<FaceRegisterResponseDto> RegisterFaceAsync(FaceRegisterRequestDto request);
    Task<FaceIdentifyResponseDto> IdentifyFaceAsync(IFormFile image);
    float[] GetEmbedding(string imagePath);
    float CosineSimilarity(float[] vec1, float[] vec2);
}

