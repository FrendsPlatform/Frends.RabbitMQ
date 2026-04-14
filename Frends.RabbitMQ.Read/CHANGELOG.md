# Changelog

## [2.2.0] - 2026-04-14
### Added
- Added Options class with ThrowErrorOnFailure and ErrorMessageOnFailure fields to control error handling behavior.

## [2.1.0] - 2026-03-17
### Added
- Added CertificateWithCredentials authentication method that requires both a valid client certificate and username/password credentials to connect.
- Added Certificate authentication method for TLS client-certificate authentication with new enums (`CertificateSource`, `SslProtocol`, `CertificateStoreLocation`) and connection properties (`ClientCertificatePath`, `ClientCertificatePassword`, `CertificateBase64`, `CertificateBytes`, `StoreThumbprint`, `CertificateStoreLocation`, `SslProtocol`, `CertificateSource`).
- Added VirtualHost field to Connection parameters to allow specifying a RabbitMQ virtual host. Defaults to "/".

## [2.0.0] - 2026-02-23

### Changed

- Acknowledgement of messages logic was simplified.
- Types of acknowledge actions were clarified.
- **[Breaking Change]** Rename ManualAck to NoAck as it's more descriptive (ack is not set with this option and can't be
  sent manually within this task)
- **[Breaking Change]** Rename `Connection.AutoAck` property to `Connection.AckType`

## [1.1.0] - 2025-02-14

### Fixed

- Update RabbitMQ.Client from 6.4.0 to 7.1.2

## [1.0.2] - 2023-03-14

### Fixed

- Fixed issue with connections and channels were left open by implementing IDisposable class in ConnectionHelper class.

### Added

- Added support for reading messages from quorum queues.

## [1.0.1] - 2022-10-12

### Added

- New property: Result.Success to indicate that the Read task was successfully completed.

## [1.0.0] - 2022-08-22

### Added

- Initial implementation
