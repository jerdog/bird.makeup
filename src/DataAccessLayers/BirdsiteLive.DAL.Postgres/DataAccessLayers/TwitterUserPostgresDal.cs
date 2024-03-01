﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.DAL.Postgres.DataAccessLayers.Base;
using BirdsiteLive.DAL.Postgres.Settings;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace BirdsiteLive.DAL.Postgres.DataAccessLayers
{
    public class TwitterUserPostgresDal : SocialMediaUserPostgresDal, ITwitterUserDal
    {
        #region Ctor
        public TwitterUserPostgresDal(PostgresSettings settings) : base(settings)
        {

            tableName = _settings.TwitterUserTableName;
        }
        #endregion

        public override string tableName { get; set; } 
        public override string FollowingColumnName { get; set; } = "followings";

        public async Task<TimeSpan> GetTwitterSyncLag()
        {
            var query = $"SELECT max(lastsync) - min(lastsync) as diff FROM (SELECT unnest(followings) as follow FROM followers GROUP BY follow) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id;";

            using (var dbConnection = Connection)
            {
                var result = (await dbConnection.QueryAsync<TimeSpan?>(query)).FirstOrDefault() ?? TimeSpan.Zero;
                return result;
            }
        }

        public async Task<int> GetTwitterUsersCountAsync()
        {
            var query = $"SELECT COUNT(*) FROM (SELECT unnest(followings) as follow FROM {_settings.FollowersTableName} GROUP BY follow) AS f INNER JOIN {_settings.TwitterUserTableName} ON f.follow={_settings.TwitterUserTableName}.id";

            using (var dbConnection = Connection)
            {
                var result = (await dbConnection.QueryAsync<int>(query)).FirstOrDefault();
                return result;
            }
        }

        public async Task<int> GetFailingTwitterUsersCountAsync()
        {
            var query = $"SELECT COUNT(*) FROM {_settings.TwitterUserTableName} WHERE fetchingErrorCount > 0";

            using (var dbConnection = Connection)
            {
                var result = (await dbConnection.QueryAsync<int>(query)).FirstOrDefault();
                return result;
            }
        }

        public async Task<SyncTwitterUser[]> GetAllTwitterUsersWithFollowersAsync(int maxNumber, int nStart, int nEnd, int m)
        {
            const string query = @"
                WITH following AS (
                    SELECT unnest(followings) as follow FROM followers
                ),
                following2 AS (
                    SELECT id, lastsync, count(*) as followers FROM following
                    INNER JOIN twitter_users ON following.follow=twitter_users.id
                    WHERE mod(id, $2) >= $3 AND mod(id, $2) <= $4
                    GROUP BY id
                    ORDER BY lastSync ASC NULLS FIRST
                    LIMIT $1
                )
                UPDATE twitter_users
                SET lastsync = NOW()
                FROM following2
                WHERE following2.id = twitter_users.id
                RETURNING * 
                ";

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters =
                {
                    new() { Value = maxNumber},
                    new() { Value = m},
                    new() { Value = nStart},
                    new() { Value = nEnd}
                }
            };
            var reader = await command.ExecuteReaderAsync();
            var results = new List<SyncTwitterUser>();
            while (await reader.ReadAsync())
            {
                results.Add(new SyncTwitterUser
                    {
                        Id = reader["id"] as int? ?? default,
                        Acct = reader["acct"] as string,
                        TwitterUserId = reader["twitterUserId"] as long? ?? default,
                        LastTweetPostedId = reader["lastTweetPostedId"] as long? ?? default,
                        LastSync = reader["lastSync"] as DateTime? ?? default,
                        FetchingErrorCount = reader["fetchingErrorCount"] as int? ?? default,
                        Followers = reader["followers"] as long? ?? default,
                        StatusesCount = reader["statusescount"] as int? ?? -1,
                    }
                );

            }
            return results.ToArray();
        }

        public async Task<SyncTwitterUser[]> GetAllTwitterUsersAsync(int maxNumber)
        {
            var query = $"SELECT * FROM {_settings.TwitterUserTableName} ORDER BY lastSync ASC NULLS FIRST LIMIT @maxNumber";

            using (var dbConnection = Connection)
            {
                var result = await dbConnection.QueryAsync<SyncTwitterUser>(query, new { maxNumber });
                return result.ToArray();
            }
        }

        public async Task<SyncTwitterUser[]> GetAllTwitterUsersAsync()
        {
            var query = $"SELECT * FROM {_settings.TwitterUserTableName}";

            using (var dbConnection = Connection)
            {
                var result = await dbConnection.QueryAsync<SyncTwitterUser>(query);
                return result.ToArray();
            }
        }

        public async Task UpdateTwitterUserFediAcctAsync(string twitterUsername, string key)
        {
            if(twitterUsername == default) throw new ArgumentException("id");

            var query = $"UPDATE {_settings.TwitterUserTableName} " + "SET extradata['fediaccount'] = $1 WHERE acct = $2";
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = JsonSerializer.Serialize(key), NpgsqlDbType = NpgsqlDbType.Jsonb },
                    new() { Value = twitterUsername},
                    
                }
            };

            await command.ExecuteNonQueryAsync();
        }
        public async Task UpdateUserExtradataAsync(string twitterUsername, string key, object value)
        {
            if(twitterUsername == default) throw new ArgumentException("id");

            var query = $"UPDATE {_settings.TwitterUserTableName} SET extradata['wikidata'][$1] = $2 WHERE acct = $3";
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = 
                { 
                    new() { Value = key},
                    new() { Value = JsonSerializer.Serialize(value), NpgsqlDbType = NpgsqlDbType.Jsonb },
                    new() { Value = twitterUsername},
                    
                }
            };

            await command.ExecuteNonQueryAsync();
        }
        public async Task UpdateTwitterUserIdAsync(string username, long twitterUserId)
        {
            if(username == default) throw new ArgumentException("id");
            if(twitterUserId == default) throw new ArgumentException("twtterUserId");

            var query = $"UPDATE {_settings.TwitterUserTableName} SET twitterUserId = $1 WHERE acct = $2";
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = { new() { Value = twitterUserId}, new() { Value = username}}
            };

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateTwitterStatusesCountAsync(string username, long StatusesCount)
        {
            if(username == default) throw new ArgumentException("id");
            if(StatusesCount == default) throw new ArgumentException("statuses count");

            var query = $"UPDATE {_settings.TwitterUserTableName} SET statusescount = $1 WHERE acct = $2";
            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = { new() { Value = StatusesCount}, new() { Value = username}}
            };

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateTwitterUserAsync(int id, long lastTweetPostedId, int fetchingErrorCount, DateTime lastSync)
        {
            if(id == default) throw new ArgumentException("id");
            if(lastTweetPostedId == default) throw new ArgumentException("lastTweetPostedId");
            if(lastSync == default) throw new ArgumentException("lastSync");

            var query = $"UPDATE {_settings.TwitterUserTableName} SET lastTweetPostedId = $1, fetchingErrorCount = $2, lastSync = $3 WHERE id = $4";

            await using var connection = DataSource.CreateConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection) {
                Parameters = { 
                    new() { Value = lastTweetPostedId}, 
                    new() { Value = fetchingErrorCount},
                    new() { Value = lastSync.ToUniversalTime()},
                    new() { Value = id},
                }
            };

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateTwitterUserAsync(SyncTwitterUser user)
        {
            await UpdateTwitterUserAsync(user.Id, user.LastTweetPostedId, user.FetchingErrorCount, user.LastSync);
        }

    }
}