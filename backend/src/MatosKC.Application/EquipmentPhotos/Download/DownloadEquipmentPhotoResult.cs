namespace MatosKC.Application.EquipmentPhotos.Download;

using MatosKC.Application.Files.Ports;

public sealed record DownloadEquipmentPhotoResult(
    StoredObject Object,
    string FileName
);
