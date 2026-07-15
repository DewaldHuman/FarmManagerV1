namespace Farm.Web.Core.Registry;

public interface IFarmService
{
    Task<FarmDto?> GetFarmAsync();

    Task<List<FieldDto>> ListFieldsAsync();
    Task<FieldDto?> GetFieldAsync(Guid id);
    Task<FieldDto?> CreateFieldAsync(FieldCreateRequest request);
    Task<FieldDto?> UpdateFieldAsync(Guid id, FieldUpdateRequest request);
    Task<bool> ArchiveFieldAsync(Guid id);

    Task<List<ZoneDto>> ListZonesAsync(Guid? fieldId = null);
    Task<ZoneDto?> GetZoneAsync(Guid id);
    Task<ZoneDto?> CreateZoneAsync(ZoneCreateRequest request);
    Task<ZoneDto?> UpdateZoneAsync(Guid id, ZoneUpdateRequest request);
    Task<bool> ArchiveZoneAsync(Guid id);

    Task<SettingsDto?> GetSettingsAsync();
    Task<SettingsDto?> UpdateSettingsAsync(SettingsUpdateRequest request);
}
