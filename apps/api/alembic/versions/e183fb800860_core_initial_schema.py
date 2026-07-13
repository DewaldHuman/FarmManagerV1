"""core: initial schema

Revision ID: e183fb800860
Revises:
Create Date: 2026-07-13

"""
from typing import Sequence, Union

from alembic import op

# revision identifiers, used by Alembic.
revision: str = "e183fb800860"
down_revision: Union[str, None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.execute("CREATE SCHEMA IF NOT EXISTS core")


def downgrade() -> None:
    op.execute("DROP SCHEMA IF EXISTS core CASCADE")
