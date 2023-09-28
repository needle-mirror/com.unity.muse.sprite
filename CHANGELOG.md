# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2023-09-28

### Added

- Feedback mechanism.

### Changed

- Style Trainer cache DB moved to project's library folder in Unity Editor.
- Changed star icon in version drop down to better reflect the selected version.

### Fixed

- Remove special characters from suggested export name.
- Doodle pad works after entering-exiting playmode.
- Reduce image preloading in Style Trainer.
- Fix unable to load default style in when default style project goes into a weird state.
- Remove incorrect icons in the style dropdown.
- Fix index out of range exception in style version drop down.

## [0.2.0] - 2023-09-20

### Changed

- Reduce error logging.
- Style selection is now part of the Sprite Generation operator. Old Sprite Muse asset will enounter exception and might not be usable. Please start with a new project.

### Fixed

- Dragging and dropping elements that are outside of the GridView's ScrollView to the Project, or Scene view.
- Fix style selection using incorrect settings.
- Fix grid view alignment in style trainer.
- Fix empty name and description allowed in style trainer.


## [0.1.2] - 2023-09-12

## [0.1.1] - 2023-08-28

### Added

- Added style training capabilities.
- Allow users to select custom trained style for generation.

## [0.1.0] - 2023-06-10

### Added

- Initial release of the Unity Muse AI Tools package.
