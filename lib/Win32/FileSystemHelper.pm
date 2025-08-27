package Win32::FileSystemHelper;

use strict;
use warnings;
use utf8;

our $VERSION = 0.02;

use FFI::Platypus;
use FFI::CheckLib qw( find_lib_or_die );

my $ffi = FFI::Platypus->new( api => 2, lib => find_lib_or_die( lib => 'Win32_FileSystemHelper' ) );

$ffi->attach( ['FreeMemory' => 'free_memory'] => ['opaque'] => 'void' );
$ffi->attach( ['Initialize' => 'initialize'] => ['string', 'string'] => 'void' );
$ffi->attach( ['Event' => 'event'] => ['void'] => 'opaque' => sub {
    my ($xsub) = @_;

    my $ptr = $xsub->();

    my $str = $ffi->cast( 'opaque' => 'string', $ptr );

    free_memory($ptr);

    return $str;
});
$ffi->attach( ['EventBlocking' => 'event_blocking'] => ['void'] => 'opaque' => sub {
    my ($xsub) = @_;

    my $ptr = $xsub->();

    my $str = $ffi->cast( 'opaque' => 'string', $ptr );

    free_memory($ptr);

    return $str;
});
$ffi->attach( ['Cleanup' => 'cleanup'] => ['void'] => 'void' );
$ffi->attach( ['Interrupt' => 'interrupt'] => ['void'] => 'void' );
$ffi->attach( ['GetFullPath' => 'get_full_path'] => ['string'] => 'opaque' => sub {
    my ($xsub, $path) = @_;

    my $ptr = $xsub->($path);

    my $str = $ffi->cast( 'opaque' => 'string', $ptr );

    free_memory($ptr);

    return $str;
});
$ffi->attach( ['GetShortPath' => 'get_short_path'] => ['string'] => 'opaque' => sub {
    my ($xsub, $path) = @_;

    my $ptr = $xsub->($path);

    my $str = $ffi->cast( 'opaque' => 'string', $ptr );

    free_memory($ptr);

    return $str;
});
