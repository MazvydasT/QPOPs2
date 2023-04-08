# QPOPs2
Command line tool to convert Process Designer XML files to JTs.

### USAGE:

    Create individually named JT files:
      QPOPs2 -i C:\input1.xml C:\input2.xml -o C:\output1.jt C:\output2.jt -s P:\ath\to\sys_root

    Create JT files named after input files in a single output folder:
      QPOPs2 --input=C:\input1.xml C:\input2.xml --output=C:\output\folder --sysroot=P:\ath\to\sys_root

    Create JT file with product data only:
      QPOPs2 --input=C:\input1.xml --resource=False --sysroot=P:\ath\to\sys_root

    Create JT file with resource data only and branches without CAD included:
      QPOPs2 -c -i C:\input1.xml -p False -s P:\ath\to\sys_root

      -s, --sysroot                                     Required. Path to sys_root folder.

      -i, --input                                       Required. Paths to input XML files.

      -o, --output                                      Paths to output JT files, or path to an output folder.
                                                        If not provided output folder is the same as input file folder.

      -p, --product                                     (Default: true) Outputs product data to JT file.

      -r, --resource                                    (Default: true) Outputs resource data to JT file.

      -c, --include-branches-without-cad                (Default: false) Outputs parts of the tree without CAD to JT file.

      -a, --resource-sysroot-jt-files-are-assemblies    (Default: true) Resource JT files under sys_root are assemblies,
                                                        not under sys_root - parts.

      --help                                            Display this help screen.

      --version                                         Display version information.
