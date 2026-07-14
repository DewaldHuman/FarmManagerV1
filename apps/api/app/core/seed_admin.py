import argparse
import getpass
import sys

from app.core.models import Role, User
from app.core.security import hash_password
from app.db import SessionLocal


def seed_admin(username: str, display_name: str, password: str) -> None:
    db = SessionLocal()
    try:
        existing = db.query(User).filter(User.username == username).first()
        if existing is not None:
            print(f"User '{username}' already exists — aborting, no changes made.", file=sys.stderr)
            sys.exit(1)

        any_user = db.query(User).first()
        if any_user is not None:
            print(
                "A user already exists in core.users. This script is only for creating "
                "the FIRST owner account. Aborting.",
                file=sys.stderr,
            )
            sys.exit(1)

        user = User(
            username=username,
            display_name=display_name,
            hashed_password=hash_password(password),
            role=Role.OWNER,
            is_active=True,
        )
        db.add(user)
        db.commit()
        print(f"Created owner user '{username}'.")
    finally:
        db.close()


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed the first owner account for FarmManager.")
    parser.add_argument("--username", required=True)
    parser.add_argument("--display-name", required=True)
    parser.add_argument(
        "--password",
        help="Skip the interactive prompt (for scripted/demo use only — "
        "avoid for real accounts, since it can land in shell history).",
    )
    args = parser.parse_args()

    if args.password:
        password = args.password
    else:
        password = getpass.getpass("Password: ")
        password_confirm = getpass.getpass("Confirm password: ")
        if password != password_confirm:
            print("Passwords do not match.", file=sys.stderr)
            sys.exit(1)

    seed_admin(args.username, args.display_name, password)


if __name__ == "__main__":
    main()
