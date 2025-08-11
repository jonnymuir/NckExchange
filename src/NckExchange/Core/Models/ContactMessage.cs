using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using System;

namespace NckExchange.Core.Models;

[TableName("ContactMessages")] // The name of your custom table
[PrimaryKey("Id", AutoIncrement = true)] // Define the primary key
[ExplicitColumns] // Only map properties with [Column] attribute
public class ContactMessage
{
    [PrimaryKeyColumn(AutoIncrement = true)]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty; // Initialize to avoid null warnings

    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Column("Message")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)] // For potentially long messages
    public string Message { get; set; } = string.Empty;

    [Column("DateSubmitted")]
    public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

    [Column("IsAnswered")]
    public bool IsAnswered { get; set; } = false; // Flag for whether the message has been answered

    [Column("Answer")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    [NullSetting(NullSetting = NullSettings.Null)] // This column can be null initially
    public string? Answer { get; set; }

    [Column("DateAnswered")]
    [NullSetting(NullSetting = NullSettings.Null)] // This column can be null initially
    public DateTime? DateAnswered { get; set; }
}
