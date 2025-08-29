package Win32::LongPath::Path;

use strict;
use warnings;
use utf8;

use Encode;

use Win32::LongPath;

use overload
    '""' => \&str,
    '-X' => \&filetests,
    "fallback" => 1;

sub new {
    my $p = shift;
    bless \@_, $p;
}

sub filetests {
    my $p = shift;
    return testL( $_[0], decode_utf8( $p->[0] ) );
}

sub str {
    my $p = shift;
    return $p->[0];
}

1;