module.exports = {
  extends: ['@commitlint/config-conventional'],
  parserPreset: {
    parserOpts: {
      headerPattern: /^(\w*)(?:\((.*)\))?!?:\s*(.*)$/,
      headerCorrespondence: ['type', 'scope', 'subject'],
    },
  },
  ignores: [(msg) => msg.includes('Signed-off-by: dependabot[bot]')],
};
