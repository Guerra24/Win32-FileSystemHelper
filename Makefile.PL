use ExtUtils::MakeMaker;

WriteMakefile(
  NAME => 'Win32::FileSystemHelper',
  VERSION_FROM => 'lib/Win32/FileSystemHelper.pm',
  PREREQ_PM => {
    'FFI::Platypus' => '2.10',
    'FFI::CheckLib' => '0.31',
  },
  AUTHOR => 'Guerra24',
);