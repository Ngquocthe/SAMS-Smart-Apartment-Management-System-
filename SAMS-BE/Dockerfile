FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

# Ép .NET listen 80
ENV ASPNETCORE_URLS=http://+:80

# Cài native libs cần thiết cho OpenCvSharp + Tesseract
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    tesseract-ocr libtesseract-dev libleptonica-dev \
    libopenexr25 libopenexr-dev \
    libgtk2.0-0 libglib2.0-0 libsm6 libxrender1 libxext6 \
    libjpeg-turbo8 libpng16-16 libtiff5 \
    libgomp1 \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY publish/ .

# đảm bảo thư viện native có thể load (tùy trường hợp)
ENV LD_LIBRARY_PATH=/app/runtimes/linux-x64/native:$LD_LIBRARY_PATH

EXPOSE 80
ENTRYPOINT ["dotnet", "SAMS-BE.dll"]
