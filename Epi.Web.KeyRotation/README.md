# Epi.Web.KeyRotation

Web Enter encrypts its connection strings, the `AdminKey` setting and the organization keys stored in
`dbo.Organization` with key material from `appSettings`, and derives users' password hashes from
`KeyForUserPasswordSalt`. Each deployment must use its own values: the applications refuse to start when the
`KeyForConnectionString*` settings are missing, malformed or publicly known values, or when
`KeyForUserPasswordSalt` is set to a publicly known value.

This console tool creates deployment key material and moves an existing deployment onto it. It runs on
.NET Framework 4.8 and is built with the solution (`Epi.Web.KeyRotation.exe`).

## Settings

| Setting | Purpose |
| --- | --- |
| `KeyForConnectionStringPassphrase`, `KeyForConnectionStringSalt`, `KeyForConnectionStringVector` | Required. The vector must be exactly 16 ASCII characters. |
| `AdminKey` | Optional. The encrypted admin key GUID that the manager service checks before organization operations. Leave it empty to disable them. |
| `KeyForUserPasswordSalt` | Required wherever users sign in with a password. At least 8 bytes; every configuration that shares the database must use the same value. |
| `LegacyKeyForUserPasswordSalt` | Only during a password-salt migration: the previous salt. Users whose hash still uses it are re-hashed with `KeyForUserPasswordSalt` when they sign in. |

## New deployment

```powershell
.\Epi.Web.KeyRotation.exe generate --out D:\secure\ewe.keys
'Data Source=...;Initial Catalog=...;Integrated Security=True' | .\Epi.Web.KeyRotation.exe encrypt --keys D:\secure\ewe.keys
.\Epi.Web.KeyRotation.exe encrypt --keys D:\secure\ewe.keys --admin-key
```

Copy the key settings, `KeyForUserPasswordSalt` and the encrypted values into each `Web.config`, store the key
file's values in your secret store and delete the file. Give the `AdminKeyGuid` only to administrators who
manage organizations.

## Existing deployment

1. Back up the database and every `Web.config` that shares it: the Web Enter site and the publishing service.
2. Stop the application pools for those sites.
3. Generate key material once: `.\Epi.Web.KeyRotation.exe generate --out D:\secure\ewe.keys`.
4. Dry run the configuration that holds the database connection string, once per database:

   ```powershell
   .\Epi.Web.KeyRotation.exe rotate --config <site>\Web.config --new-keys D:\secure\ewe.keys --database EIWSEntities
   ```

   The old keys are read from the configuration. When the hosting environment supplies the
   `KeyForConnectionString*` settings instead, put the current values in a file (same `name=value` format as the
   generated file) and pass it with `--old-keys`. The report lists every setting it would change and never
   prints key or connection-string values. Nothing is written without `--apply`.
5. Re-run the same command with `--apply`. The organization keys are re-encrypted in one transaction and
   checked before commit; the configuration is written afterwards, with a backup named
   `Web.<timestamp>.keyrotation-backup.config` next to it.
6. Rotate every other configuration of the deployment with the same key file, without `--database`.
7. Start the application pools and check that the sites work and that users can sign in. Then delete the
   backups and the key file.

Organization keys held by users don't change; only their stored encryption does. If the database step
succeeded but the configuration wasn't written, run the same command again: rows that already use the new
keys are recognized and left alone. If a configuration was rotated without `--database`, re-running with
`--database` checks every organization key against the new keys and reports what to do; pass the previous
keys with `--old-keys` to re-encrypt rows that still use them. The tool refuses to run when a row or an
encrypted connection string can't be decrypted with either key set, or when a section uses `configSource`.
Plain-text connection strings (for example `ApplicationServices`) are reported and left unchanged.

`AdminKey` is replaced with the key file's `AdminKeyGuid`. To keep the existing admin key, remove
`AdminKeyGuid` from the key file; a publicly known admin key is never kept.

## Password salt migration

Password hashes can't be re-computed without the users' passwords, so a publicly known
`KeyForUserPasswordSalt` is moved rather than replaced outright: `rotate` copies it to
`LegacyKeyForUserPasswordSalt` and sets `KeyForUserPasswordSalt` to the key file's value. When a user signs in,
the application tries the new salt first, then the legacy salt; on a legacy match it stores a new hash made
with the new salt. Password resets and new accounts always use the new salt. A deployment-specific
salt is kept as is.

Keep `LegacyKeyForUserPasswordSalt` until the users who matter have signed in. While it is set, hashes that
still use it are only as strong as before the rotation. Once you remove it, users who haven't signed in since
can't sign in with their old password and must use "Forgot password" to get a new one.
