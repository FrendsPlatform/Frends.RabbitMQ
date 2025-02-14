# Changelog

## [1.0.3] - 2025-02-14
### Fixed
- Update RabbitMQ.Client from 6.4.0 to 7.0.0

## [1.0.2] - 2023-03-14
### Fixed
- Fixed issue with connections and channels were left open by implementinf IDisposable class in ConnectionHelper class.

### Added
- Added support for reading messages from quorum queues.

## [1.0.1] - 2022-10-12
### Added
- New property: Result.Success to indicate that the Read task was successfully completed.

## [1.0.0] - 2022-08-22
### Added
- Initial implementation
