using eShopLegacyMVC.Models;
using eShopLegacyMVC.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace eShopLegacyMVC.Services
{
    /// <summary>
    /// Legacy implementation of ICatalogService that uses stored procedures
    /// for all data access. This represents the "before" state of many enterprise
    /// applications that rely heavily on SP-based data access patterns.
    /// 
    /// ISSUES WITH THIS APPROACH (for modernization demo):
    /// - Tight coupling to SQL Server stored procedures
    /// - Manual ADO.NET data mapping (no ORM benefits)
    /// - Synchronous-only execution (no async support)
    /// - Business logic split between C# and T-SQL
    /// - Hard to unit test (requires real database)
    /// - Connection string management via ConfigurationManager
    /// - No parameterized query builder (raw SqlCommand)
    /// - Manual IDisposable management
    /// </summary>
    public class CatalogServiceSP : ICatalogService
    {
        private readonly string _connectionString;
        private readonly CatalogDBContext _db;

        public CatalogServiceSP(CatalogDBContext db)
        {
            _db = db;
            _connectionString = db.Database.Connection.ConnectionString;
        }

        public PaginatedItemsViewModel<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex)
        {
            var items = new List<CatalogItem>();
            long totalItems = 0;

            // LEGACY PATTERN: Direct ADO.NET with stored procedure
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_GetCatalogItemsPaginated", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@PageIndex", pageIndex);

                    using (var reader = command.ExecuteReader())
                    {
                        // First result set: total count
                        if (reader.Read())
                        {
                            totalItems = reader.GetInt32(0);
                        }

                        // Second result set: paginated items
                        reader.NextResult();
                        while (reader.Read())
                        {
                            items.Add(MapCatalogItemFromReader(reader));
                        }
                    }
                }
            }

            return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, items);
        }

        public CatalogItem FindCatalogItem(int id)
        {
            // LEGACY PATTERN: Stored procedure call for single item retrieval
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_GetCatalogItemById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapCatalogItemFromReader(reader);
                        }
                    }
                }
            }
            return null;
        }

        public IEnumerable<CatalogType> GetCatalogTypes()
        {
            // Falls back to EF for simple lookups (common in legacy hybrid apps)
            return _db.CatalogTypes.ToList();
        }

        public IEnumerable<CatalogBrand> GetCatalogBrands()
        {
            // Falls back to EF for simple lookups
            return _db.CatalogBrands.ToList();
        }

        public void CreateCatalogItem(CatalogItem catalogItem)
        {
            // LEGACY PATTERN: SP with output parameter for generated ID
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_CreateCatalogItem", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Name", catalogItem.Name);
                    command.Parameters.AddWithValue("@Description", (object)catalogItem.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Price", catalogItem.Price);
                    command.Parameters.AddWithValue("@PictureFileName", catalogItem.PictureFileName ?? "dummy.png");
                    command.Parameters.AddWithValue("@CatalogTypeId", catalogItem.CatalogTypeId);
                    command.Parameters.AddWithValue("@CatalogBrandId", catalogItem.CatalogBrandId);
                    command.Parameters.AddWithValue("@AvailableStock", catalogItem.AvailableStock);
                    command.Parameters.AddWithValue("@RestockThreshold", catalogItem.RestockThreshold);
                    command.Parameters.AddWithValue("@MaxStockThreshold", catalogItem.MaxStockThreshold);
                    command.Parameters.AddWithValue("@OnReorder", catalogItem.OnReorder);

                    var newIdParam = new SqlParameter("@NewId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(newIdParam);

                    command.ExecuteNonQuery();
                    catalogItem.Id = (int)newIdParam.Value;
                }
            }
        }

        public void UpdateCatalogItem(CatalogItem catalogItem)
        {
            // LEGACY PATTERN: Full entity update via stored procedure
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_UpdateCatalogItem", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", catalogItem.Id);
                    command.Parameters.AddWithValue("@Name", catalogItem.Name);
                    command.Parameters.AddWithValue("@Description", (object)catalogItem.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Price", catalogItem.Price);
                    command.Parameters.AddWithValue("@PictureFileName", catalogItem.PictureFileName);
                    command.Parameters.AddWithValue("@CatalogTypeId", catalogItem.CatalogTypeId);
                    command.Parameters.AddWithValue("@CatalogBrandId", catalogItem.CatalogBrandId);
                    command.Parameters.AddWithValue("@AvailableStock", catalogItem.AvailableStock);
                    command.Parameters.AddWithValue("@RestockThreshold", catalogItem.RestockThreshold);
                    command.Parameters.AddWithValue("@MaxStockThreshold", catalogItem.MaxStockThreshold);
                    command.Parameters.AddWithValue("@OnReorder", catalogItem.OnReorder);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void RemoveCatalogItem(CatalogItem catalogItem)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_DeleteCatalogItem", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", catalogItem.Id);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Updates inventory with business rule enforcement via stored procedure.
        /// The SP handles:
        /// - Negative stock prevention
        /// - Max stock threshold enforcement
        /// - Automatic OnReorder flag management based on RestockThreshold
        /// </summary>
        public InventoryUpdateResult UpdateInventory(int catalogItemId, int quantityChange)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("sp_UpdateInventory", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Id", catalogItemId);
                    command.Parameters.AddWithValue("@QuantityChange", quantityChange);

                    var updatedStockParam = new SqlParameter("@UpdatedStock", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var isOnReorderParam = new SqlParameter("@IsOnReorder", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(updatedStockParam);
                    command.Parameters.Add(isOnReorderParam);

                    command.ExecuteNonQuery();

                    return new InventoryUpdateResult
                    {
                        CatalogItemId = catalogItemId,
                        UpdatedStock = (int)updatedStockParam.Value,
                        IsOnReorder = (bool)isOnReorderParam.Value,
                        QuantityChanged = quantityChange
                    };
                }
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }

        /// <summary>
        /// LEGACY PATTERN: Manual data reader mapping - error-prone, not type-safe,
        /// and requires exact column order knowledge. This is a prime candidate for
        /// EF Core migration which handles this automatically.
        /// </summary>
        private CatalogItem MapCatalogItemFromReader(SqlDataReader reader)
        {
            return new CatalogItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                PictureFileName = reader.GetString(reader.GetOrdinal("PictureFileName")),
                CatalogTypeId = reader.GetInt32(reader.GetOrdinal("CatalogTypeId")),
                CatalogType = new CatalogType { Type = reader.GetString(reader.GetOrdinal("CatalogTypeName")) },
                CatalogBrandId = reader.GetInt32(reader.GetOrdinal("CatalogBrandId")),
                CatalogBrand = new CatalogBrand { Brand = reader.GetString(reader.GetOrdinal("CatalogBrandName")) },
                AvailableStock = reader.GetInt32(reader.GetOrdinal("AvailableStock")),
                RestockThreshold = reader.GetInt32(reader.GetOrdinal("RestockThreshold")),
                MaxStockThreshold = reader.GetInt32(reader.GetOrdinal("MaxStockThreshold")),
                OnReorder = reader.GetBoolean(reader.GetOrdinal("OnReorder"))
            };
        }
    }

    /// <summary>
    /// Result of an inventory update operation via sp_UpdateInventory.
    /// This encapsulates the business logic output that was previously
    /// hidden inside the stored procedure.
    /// </summary>
    public class InventoryUpdateResult
    {
        public int CatalogItemId { get; set; }
        public int UpdatedStock { get; set; }
        public bool IsOnReorder { get; set; }
        public int QuantityChanged { get; set; }
    }
}
