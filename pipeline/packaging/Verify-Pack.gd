extends SceneTree

# Run using the exported executable with --script and --check-list <absolute JSON>.
func _initialize():
    var args = OS.get_cmdline_user_args()
    var index = args.find("--check-list")
    if index < 0 or index + 1 >= args.size():
        push_error("Missing --check-list")
        quit(2)
        return
    var paths = JSON.parse_string(FileAccess.get_file_as_string(args[index + 1]))
    var missing = []
    var invalid_json = []
    for path in paths:
        if path.ends_with(".json"):
            if not FileAccess.file_exists(path):
                missing.append(path)
            else:
                var parser = JSON.new()
                if parser.parse(FileAccess.get_file_as_string(path)) != OK:
                    invalid_json.append(path)
        elif not ResourceLoader.exists(path):
            missing.append(path)
    print(JSON.stringify({"checked": paths.size(), "missing": missing, "invalid_json": invalid_json}))
    quit(0 if missing.is_empty() and invalid_json.is_empty() else 1)
