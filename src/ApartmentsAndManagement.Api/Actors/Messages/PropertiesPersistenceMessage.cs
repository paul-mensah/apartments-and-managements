using ApartmentsAndManagement.Api.Storage;

namespace ApartmentsAndManagement.Api.Actors.Messages;

public struct PropertiesPersistenceMessage
{
    public List<PropertyData> Documents { get; }

    public PropertiesPersistenceMessage(List<PropertyData> documents)
    {
        Documents = documents;
    }
}