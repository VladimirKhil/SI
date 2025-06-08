namespace SIData;

public enum AuthenticationResult
{
    Ok,
    Forbidden,
    ForbiddenRole,
    WrongPassword,
    NameIsOccupied,
    PositionNotFound,
    PlaceIsOccupied,
    FreePlaceNotFound,
}
