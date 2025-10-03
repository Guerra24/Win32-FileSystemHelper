package Win32::LongPath::Find;

use strict;
use warnings;
use utf8;
use feature qw(signatures);
no warnings 'experimental::signatures';

use Win32::LongPath qw(:funcs :fileattr);

sub find( $path ) {
    my @files;

    my $dir = Win32::LongPath->new();
    $dir->opendirL( $path ) or die ("unable to open $path ($^E)");
    foreach my $file ($dir->readdirL()) {
        next if ($file eq '..' or $file eq '.');

        my $name = $file eq '.' ? $path : "$path/$file";
        my $stat = lstatL( $name ) or die "unable to stat $name ($^E)";

        if (($stat->{attribs} & (FILE_ATTRIBUTE_DIRECTORY | FILE_ATTRIBUTE_REPARSE_POINT)) == FILE_ATTRIBUTE_DIRECTORY) {
            push @files, find( $name );
            next;
        }

        push @files, $name;
    }
    $dir->closedirL();
    return @files;
}

1;