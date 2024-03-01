using System;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.DAL.Postgres.DataAccessLayers.Base;
using BirdsiteLive.DAL.Postgres.Settings;
using Npgsql;
using NpgsqlTypes;

namespace BirdsiteLive.DAL.Postgres.DataAccessLayers;

public abstract class SocialMediaUserPostgresDal : PostgresBase, SocialMediaUserDal
{
    
        #region Ctor
        public SocialMediaUserPostgresDal(PostgresSettings settings) : base(settings)
        {
            
        }
        #endregion

        public abstract string tableName { get; set; }
        public abstract string FollowingColumnName { get; set; }

        public async Task<SyncUser> GetUserAsync(int id)
        {
            return await GetUserAsync(null, id);
        }
        public async Task<SyncUser> GetUserAsync(string acct)
        {
            return await GetUserAsync(acct, null);
        }
        private async Task<SyncUser> GetUserAsync(string acct, int? id)
        {
            var query = $"SELECT * FROM {tableName} WHERE acct = $1 OR id = $2";

            if (acct is not null)
                acct = acct.ToLowerInvariant();

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters =
                {
                    new() { Value = (object) acct ?? DBNull.Value},
                    new() { Value = (object) id ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Integer}
                },
            };
            var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;
            
            var extradata = JsonDocument.Parse(reader["extradata"] as string ?? "{}").RootElement;
            return new SyncTwitterUser
            {
                Id = reader["id"] as int? ?? default,
                Acct = reader["acct"] as string,
                TwitterUserId = reader["twitterUserId"] as long? ?? default,
                LastTweetPostedId = reader["lastTweetPostedId"] as long? ?? default,
                LastSync = reader["lastSync"] as DateTime? ?? default,
                FetchingErrorCount = reader["fetchingErrorCount"] as int? ?? default,
                FediAcct = reader["fediverseaccount"] as string,
                ExtraData = extradata,
            };

        }
        public async Task<long> GetFollowersCountAsync(int id)
        {
            var query = $"SELECT COUNT({FollowingColumnName}) FROM {_settings.FollowersTableName} WHERE {FollowingColumnName} @> ARRAY[$1]";

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters =
                {
                    new() { Value = id },
                },
            };
            var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            
            var count = reader["count"] as long ? ?? default ;
            return count;
        }
        public async Task DeleteUserAsync(string acct)
        {
            if (string.IsNullOrWhiteSpace(acct)) throw new ArgumentException("acct");

            acct = acct.ToLowerInvariant();

            var query = $"DELETE FROM {tableName} WHERE acct = $1";

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = acct },
                }
            };

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            if (id == default) throw new ArgumentException("id");
            
            var query = $"DELETE FROM {tableName} WHERE id = $1";

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = id },
                }
            };

            await command.ExecuteNonQueryAsync();
        }
        public async Task CreateUserAsync(string acct)
        {
            acct = acct.ToLowerInvariant();

            var query = $"INSERT INTO {tableName} (acct,lastTweetPostedId) VALUES($1,-1)";
            // TODO improve this
            if (tableName == _settings.InstagramUserTableName)
                query = $"INSERT INTO {tableName} (acct) VALUES($1)";
                
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = acct },
                }
            };
            
            await command.ExecuteNonQueryAsync();
        }
        public async Task AddFollower(int follower, int followed)
        {

            var query = $"UPDATE {_settings.FollowersTableName} SET {FollowingColumnName} = array_append({FollowingColumnName}, $2) WHERE id = $1";
                
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = follower },
                    new() { Value = followed },
                }
            };
            
            await command.ExecuteNonQueryAsync();
        }
        public async Task RemoveFollower(int follower, int followed)
        {

            var query = $"UPDATE {_settings.FollowersTableName} SET {FollowingColumnName} = array_remove({FollowingColumnName}, $2) WHERE id = $1";
                
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = follower },
                    new() { Value = followed },
                }
            };
            
            await command.ExecuteNonQueryAsync();
        }
}