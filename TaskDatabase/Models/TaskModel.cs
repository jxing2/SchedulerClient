using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskDatabase.Model
{
    public class TaskModel
    {
        public virtual int Tid { get; set; }
        public virtual int Id { get; set; }
        public virtual string Task_name { get; set; }
        public virtual string Task_cmd { get; set; }
        public virtual string Task_param { get; set; }
        public virtual int Task_status { get; set; }
        public virtual string Task_result_json { get; set; }
        public virtual long Task_expected_finish_time { get; set; }
        public virtual DateTime Task_add_time { get; set; }
        public virtual ICollection<DownloadTaskModel> DownloadTaskModelList { get; set; }
    }

    public class DownloadTaskModel
    {
        public virtual int Tid { get; set; }
        public virtual int Id { get; set; }
        public virtual string Task_id { get; set; }
        public virtual string Task_url { get; set; }
        public virtual string Task_md5 { get; set; }
        public virtual string Local_dir { get; set; }
        public virtual long Downloaded_bytes { get; set; }
        public virtual long File_bytes { get; set; }
        public virtual long Finish_time { get; set; }
    }
}
