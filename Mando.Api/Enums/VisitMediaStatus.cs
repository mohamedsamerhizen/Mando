namespace Mando.Api.Enums;

public enum VisitMediaStatus
{
    Success = 0,
    VisitNotFound = 1,
    VisitImageNotFound = 2,
    Forbidden = 3,
    VisitNotInProgress = 4,
    ImageFileRequired = 5,
    InvalidImageType = 6,
    ImageSizeExceeded = 7,
    MaxVisitImagesReached = 8,
    VisitImageDeletionNotAllowed = 9
}
