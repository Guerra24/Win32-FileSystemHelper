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

sub MY::postamble {
    return <<'MAKE_FRAG';
all ::
	C:\Program Files\dotnet\dotnet.exe restore csharp/Win32.FileSystemHelper.csproj
	C:\Program Files\dotnet\dotnet.exe build csharp/Win32.FileSystemHelper.csproj

install ::
	C:\Program Files\dotnet\dotnet.exe publish --nologo -v:minimal -p:PublishProfile=Release csharp/Win32.FileSystemHelper.csproj
	cp csharp/bin/Release/publish/Win32_FileSystemHelper.dll $(INSTALLBIN)

clean ::
	C:\Program Files\dotnet\dotnet.exe clean --nologo -v:minimal -c Release csharp/Win32.FileSystemHelper.csproj

MAKE_FRAG
}
