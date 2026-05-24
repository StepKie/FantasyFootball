BEGIN {
  FS_ELO = "\t"; FS_CSV = ","
  m["EN"]="GB-ENG"; m["SQ"]="GB-SCT"; m["WA"]="GB-WLS"; m["EI"]="GB-NIR"
  m["NM"]="MK"; m["KO"]="XK"
  m["SW"]="SZ"; m["TI"]="PF"; m["VG"]="IO"
  new_date = "24.05.2026"
}
# Pass 1: eloratings world.tsv → new_elo[ourkey] + new_rank[ourkey]
NR == FNR {
  rank = $1; code = $3; elo_new = $4
  ourkey = (code in m) ? m[code] : code
  new_elo[ourkey] = elo_new
  new_rank[ourkey] = rank
  next
}
# Pass 2: our CSV → emit updated CSV to stdout
{
  if (FNR == 1) { print; next }
  split($0, F, ",")
  k = F[4]
  if (k in new_elo) {
    # rank, name, code3, code2, elo_old (KEEP), elo_new (UPDATE), conf, date (UPDATE), de
    printf "%d.0,%s,%s,%s,%s,%s,%s,%s,%s\n", \
      new_rank[k], F[2], F[3], F[4], F[5], new_elo[k], F[7], new_date, F[9]
    matched_count++
  } else {
    print $0  # unchanged
    unmatched_count++
  }
}
END {
  print "MATCHED: " matched_count > "/dev/stderr"
  print "UNMATCHED (kept old): " unmatched_count > "/dev/stderr"
}
